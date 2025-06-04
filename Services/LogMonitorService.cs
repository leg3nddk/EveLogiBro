using EveLogiBro.Data;
using EveLogiBro.Models;
using EveLogiBro.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EveLogiBro.Services
{
    public class LogMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogMonitorService> _logger;
        private readonly EveLogParser _logParser;
        
        // Configuration
        private string _characterName = ""; // Will be set from config or user input
        private string? _logDirectory;
        private string? _currentLogFile;
        private long _lastFilePosition = 0;
        private readonly TimeSpan _monitorInterval = TimeSpan.FromSeconds(2);

        public LogMonitorService(IServiceProvider serviceProvider, ILogger<LogMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logParser = new EveLogParser();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EVE Log Monitor Service starting...");

            // Try to load configuration
            await LoadConfiguration();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorLogFiles();
                    await Task.Delay(_monitorInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping, this is expected
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in log monitoring loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait longer on error
                }
            }

            _logger.LogInformation("EVE Log Monitor Service stopped.");
        }

        /// <summary>
        /// Main monitoring loop - checks for new log data
        /// </summary>
        private async Task MonitorLogFiles()
        {
            // Skip if no character name is configured
            if (string.IsNullOrEmpty(_characterName))
            {
                return;
            }

            // Find the latest log file for this character
            var latestLogFile = EveLogParser.FindLatestLogFile(_characterName, _logDirectory);
            
            if (latestLogFile == null)
            {
                if (_currentLogFile != null)
                {
                    _logger.LogWarning("Log file disappeared: {LogFile}", _currentLogFile);
                    _currentLogFile = null;
                    _lastFilePosition = 0;
                }
                return;
            }

            // Check if we're monitoring a new file
            if (_currentLogFile != latestLogFile)
            {
                _logger.LogInformation("Switching to new log file: {LogFile}", latestLogFile);
                _currentLogFile = latestLogFile;
                _lastFilePosition = 0;
            }

            // Read new content from the log file
            await ReadNewLogContent();
        }

        /// <summary>
        /// Read new content that has been added to the log file since last check
        /// </summary>
        private async Task ReadNewLogContent()
        {
            if (_currentLogFile == null || !File.Exists(_currentLogFile))
                return;

            try
            {
                using var fileStream = new FileStream(_currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                // Check if file has grown
                if (fileStream.Length <= _lastFilePosition)
                {
                    return; // No new content
                }

                // Seek to where we left off
                fileStream.Seek(_lastFilePosition, SeekOrigin.Begin);

                using var reader = new StreamReader(fileStream);
                var newLines = new List<string>();
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    newLines.Add(line);
                }

                // Update our position in the file
                _lastFilePosition = fileStream.Position;

                // Process new lines for repair events
                if (newLines.Count > 0)
                {
                    await ProcessNewLogLines(newLines.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log file: {LogFile}", _currentLogFile);
            }
        }

        /// <summary>
        /// Process new log lines and save any repair events to the database
        /// </summary>
        private async Task ProcessNewLogLines(string[] logLines)
        {
            var repairEvents = _logParser.ParseLogLines(logLines, _characterName);

            if (repairEvents.Count == 0)
                return;

            _logger.LogDebug("Found {Count} repair events in log", repairEvents.Count);

            // Save to database
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LogiDbContext>();

            try
            {
                // Get or create an active session for these events
                var activeSession = await GetOrCreateActiveSession(context);

                foreach (var repairEvent in repairEvents)
                {
                    // Set the session ID
                    repairEvent.SessionId = activeSession.Id;

                    // Add to database
                    context.RepairEvents.Add(repairEvent);

                    // Update session statistics
                    if (repairEvent.Direction == "Outgoing")
                        activeSession.TotalOutgoingReps++;
                    else if (repairEvent.Direction == "Incoming")
                        activeSession.TotalIncomingReps++;

                    if (repairEvent.RepairType == "Shield")
                        activeSession.TotalShieldReps++;
                    else if (repairEvent.RepairType == "Armor")
                        activeSession.TotalArmorReps++;

                    activeSession.TotalIskValue += repairEvent.IskValue;
                }

                // Calculate updated stats
                UpdateSessionStats(activeSession);

                await context.SaveChangesAsync();

                _logger.LogInformation("Saved {Count} repair events to database", repairEvents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving repair events to database");
            }
        }

        /// <summary>
        /// Get or create an active logi session
        /// </summary>
        private async Task<LogiSession> GetOrCreateActiveSession(LogiDbContext context)
        {
            var activeSession = await context.LogiSessions
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

                context.LogiSessions.Add(activeSession);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created new active logi session: {SessionId}", activeSession.Id);
            }

            return activeSession;
        }

        /// <summary>
        /// Update session statistics like RPS
        /// </summary>
        private void UpdateSessionStats(LogiSession session)
        {
            var sessionDuration = DateTime.Now - session.StartTime;
            if (sessionDuration.TotalSeconds > 0)
            {
                var totalReps = session.TotalOutgoingReps + session.TotalIncomingReps;
                session.AverageRepsPerSecond = totalReps / sessionDuration.TotalSeconds;
            }
        }

        /// <summary>
        /// Load configuration settings
        /// </summary>
        private async Task LoadConfiguration()
        {
            try
            {
                // For now, we'll use a simple approach
                // You can expand this to read from appsettings.json or a config file
                
                // Try to get default EVE log directory
                _logDirectory = EveLogParser.GetDefaultEveLogDirectory();
                
                if (Directory.Exists(_logDirectory))
                {
                    _logger.LogInformation("EVE log directory found: {LogDirectory}", _logDirectory);
                    
                    // Try to detect character name from existing log files
                    await DetectCharacterName();
                }
                else
                {
                    _logger.LogWarning("EVE log directory not found: {LogDirectory}", _logDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration");
            }
        }

        /// <summary>
        /// Try to detect the character name from existing log files
        /// </summary>
        private async Task DetectCharacterName()
        {
            try
            {
                if (_logDirectory == null || !Directory.Exists(_logDirectory))
                    return;

                // Find the most recent log file
                var logFiles = Directory.GetFiles(_logDirectory, "*.txt")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(10);

                foreach (var logFile in logFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(logFile);
                    var parts = fileName.Split('_');
                    
                    if (parts.Length >= 3)
                    {
                        var potentialCharacterName = parts[2];
                        
                        if (!string.IsNullOrEmpty(potentialCharacterName) && 
                            potentialCharacterName != "Unknown" &&
                            !potentialCharacterName.Contains("System"))
                        {
                            _characterName = potentialCharacterName;
                            _logger.LogInformation("Detected character name: {CharacterName}", _characterName);
                            return;
                        }
                    }
                }

                _logger.LogWarning("Could not detect character name from log files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting character name");
            }
        }

        /// <summary>
        /// Public method to set the character name (can be called from API)
        /// </summary>
        public void SetCharacterName(string characterName)
        {
            _characterName = characterName;
            _currentLogFile = null; // Force re-detection of log file
            _lastFilePosition = 0;
            _logger.LogInformation("Character name set to: {CharacterName}", characterName);
        }

        /// <summary>
        /// Get current monitoring status
        /// </summary>
        public object GetStatus()
        {
            return new
            {
                CharacterName = _characterName,
                LogDirectory = _logDirectory,
                CurrentLogFile = _currentLogFile,
                IsMonitoring = !string.IsNullOrEmpty(_characterName),
                LastFilePosition = _lastFilePosition
            };
        }
    }
}