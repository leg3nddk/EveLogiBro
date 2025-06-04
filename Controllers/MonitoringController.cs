using EveLogiBro.Services;
using Microsoft.AspNetCore.Mvc;

namespace EveLogiBro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly LogMonitorService _monitorService;

        public MonitoringController(IServiceProvider serviceProvider)
        {
            // Get the background service instance
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            _monitorService = hostedServices.OfType<LogMonitorService>().FirstOrDefault()
                ?? throw new InvalidOperationException("LogMonitorService not found");
        }

        // GET: api/monitoring/status
        // Returns the current status of the log monitoring service
        [HttpGet("status")]
        public ActionResult<object> GetMonitoringStatus()
        {
            var status = _monitorService.GetStatus();
            return Ok(status);
        }

        // POST: api/monitoring/character
        // Sets the character name to monitor
        [HttpPost("character")]
        public ActionResult SetCharacterName([FromBody] CharacterNameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CharacterName))
            {
                return BadRequest("Character name cannot be empty");
            }

            _monitorService.SetCharacterName(request.CharacterName);
            
            return Ok(new { 
                message = "Character name updated successfully",
                characterName = request.CharacterName
            });
        }

        // GET: api/monitoring/logdirectory
        // Returns the default EVE log directory
        [HttpGet("logdirectory")]
        public ActionResult<object> GetLogDirectory()
        {
            var defaultDirectory = EveLogParser.GetDefaultEveLogDirectory();
            var directoryExists = Directory.Exists(defaultDirectory);

            return Ok(new
            {
                directory = defaultDirectory,
                exists = directoryExists,
                accessible = directoryExists && HasDirectoryAccess(defaultDirectory)
            });
        }

        // Helper method to check if we can access a directory
        private bool HasDirectoryAccess(string directory)
        {
            try
            {
                Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // Request model for setting character name
    public class CharacterNameRequest
    {
        public string CharacterName { get; set; } = string.Empty;
    }
}