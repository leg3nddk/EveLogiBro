using EveLogiBro.Data;
using EveLogiBro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EveLogiBro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepairController : ControllerBase
    {
        private readonly LogiDbContext _context;

        // Constructor - Entity Framework will automatically inject the database context
        public RepairController(LogiDbContext context)
        {
            _context = context;
        }

        // GET: api/repair/stats
        // Returns current session statistics for the web interface
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetCurrentStats()
        {
            // Get the active session (or create one if none exists)
            var activeSession = await _context.LogiSessions
                .Where(s => s.IsActive)
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                // Return empty stats if no active session
                return Ok(new
                {
                    currentReps = 0,
                    repsPerSecond = 0.0,
                    shieldReps = 0,
                    armorReps = 0,
                    totalIskValue = 0.0m,
                    sessionActive = false
                });
            }

            // Calculate real-time statistics from the active session
            var sessionDuration = DateTime.Now - activeSession.StartTime;
            var repsPerSecond = sessionDuration.TotalSeconds > 0 
                ? activeSession.TotalOutgoingReps / sessionDuration.TotalSeconds 
                : 0.0;

            return Ok(new
            {
                currentReps = activeSession.TotalOutgoingReps,
                repsPerSecond = Math.Round(repsPerSecond, 1),
                shieldReps = activeSession.TotalShieldReps,
                armorReps = activeSession.TotalArmorReps,
                totalIskValue = activeSession.TotalIskValue,
                sessionActive = true,
                sessionDuration = Math.Round(sessionDuration.TotalMinutes, 1)
            });
        }

        // GET: api/repair/targets
        // Returns list of current targets being repaired
        [HttpGet("targets")]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrentTargets()
        {
            var activeSession = await _context.LogiSessions
                .Where(s => s.IsActive)
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                return Ok(new[] { new { name = "No active session", reps = 0 } });
            }

            // Get repair summary for each target in the current session
            var targets = await _context.RepairEvents
                .Where(r => r.SessionId == activeSession.Id && r.Direction == "Outgoing")
                .GroupBy(r => r.TargetName)
                .Select(g => new
                {
                    name = g.Key,
                    reps = g.Sum(r => r.Amount),
                    repCount = g.Count(),
                    corporation = g.First().TargetCorporation,
                    alliance = g.First().TargetAlliance,
                    shipType = g.First().TargetShipType
                })
                .OrderByDescending(t => t.reps)
                .ToListAsync();

            if (!targets.Any())
            {
                return Ok(new[] { new { name = "Waiting for combat data...", reps = 0 } });
            }

            return Ok(targets);
        }

        // POST: api/repair/event
        // Adds a new repair event to the database
        [HttpPost("event")]
        public async Task<ActionResult<RepairEvent>> CreateRepairEvent(RepairEvent repairEvent)
        {
            // Get or create active session
            var activeSession = await GetOrCreateActiveSession();
            
            // Set the session ID for this repair event
            repairEvent.SessionId = activeSession.Id;
            repairEvent.Timestamp = DateTime.Now;

            // Add the repair event to the database
            _context.RepairEvents.Add(repairEvent);

            // Update session statistics
            activeSession.TotalOutgoingReps += repairEvent.Direction == "Outgoing" ? 1 : 0;
            activeSession.TotalIncomingReps += repairEvent.Direction == "Incoming" ? 1 : 0;
            
            if (repairEvent.RepairType == "Shield")
                activeSession.TotalShieldReps++;
            else if (repairEvent.RepairType == "Armor")
                activeSession.TotalArmorReps++;

            activeSession.TotalIskValue += repairEvent.IskValue;

            // Save all changes to the database
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRepairEvent), new { id = repairEvent.Id }, repairEvent);
        }

        // GET: api/repair/event/{id}
        // Gets a specific repair event by ID
        [HttpGet("event/{id}")]
        public async Task<ActionResult<RepairEvent>> GetRepairEvent(int id)
        {
            var repairEvent = await _context.RepairEvents.FindAsync(id);

            if (repairEvent == null)
            {
                return NotFound();
            }

            return repairEvent;
        }

        // POST: api/repair/session/start
        // Starts a new logi session
        [HttpPost("session/start")]
        public async Task<ActionResult<LogiSession>> StartNewSession([FromBody] object sessionData = null)
        {
            // End any existing active sessions
            var existingSessions = await _context.LogiSessions
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var session in existingSessions)
            {
                session.IsActive = false;
                session.EndTime = DateTime.Now;
            }

            // Create new session
            var newSession = new LogiSession
            {
                StartTime = DateTime.Now,
                IsActive = true,
                SystemName = "Unknown", // We'll get this from logs later
                SystemSecurity = "Unknown",
                EngagementType = "Unknown"
            };

            _context.LogiSessions.Add(newSession);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = newSession.Id }, newSession);
        }

        // GET: api/repair/session/{id}
        // Gets a specific session by ID
        [HttpGet("session/{id}")]
        public async Task<ActionResult<LogiSession>> GetSession(int id)
        {
            var session = await _context.LogiSessions
                .Include(s => s.RepairEvents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            return session;
        }

        // Helper method to get or create an active session
        private async Task<LogiSession> GetOrCreateActiveSession()
        {
            var activeSession = await _context.LogiSessions
                .Where(s => s.IsActive)
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                activeSession = new LogiSession
                {
                    StartTime = DateTime.Now,
                    IsActive = true,
                    SystemName = "Unknown",
                    SystemSecurity = "Unknown",
                    EngagementType = "Auto-Created"
                };

                _context.LogiSessions.Add(activeSession);
                await _context.SaveChangesAsync();
            }

            return activeSession;
        }

        // GET: api/repair/sessions
        // Gets all sessions for historical view
        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<LogiSession>>> GetAllSessions()
        {
            var sessions = await _context.LogiSessions
                .OrderByDescending(s => s.StartTime)
                .Take(20) // Limit to last 20 sessions
                .ToListAsync();

            return Ok(sessions);
        }
    }
}