using EveLogiBro.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace EveLogiBro.Services
{
    public class EveLogParser
    {
        // Regex patterns for parsing different types of combat log entries
        private static readonly Regex RepairLogPattern = new Regex(
            @"\[ (?<timestamp>\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}) \] \(combat\) (?<logipilot>.+?) (?<direction>remotely repairs|repairs) (?<amount>\d+) (?<repairtype>shield|armor) damage to (?<targetname>.+?) - (?<module>.+?) - (?<system>.+?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Alternative pattern for self-repairs
        private static readonly Regex SelfRepairPattern = new Regex(
            @"\[ (?<timestamp>\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}) \] \(combat\) Your (?<module>.+?) (?<direction>repairs) (?<amount>\d+) (?<repairtype>shield|armor) damage$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Pattern for incoming repairs (when others repair you)
        private static readonly Regex IncomingRepairPattern = new Regex(
            @"\[ (?<timestamp>\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}) \] \(combat\) (?<logipilot>.+?) (?<direction>remotely repairs) (?<amount>\d+) (?<repairtype>shield|armor) damage to you - (?<module>.+?) - (?<system>.+?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Pattern for system information
        private static readonly Regex SystemInfoPattern = new Regex(
            @"Listener: (?<system>.+?) \((?<security>-?\d+\.\d+)\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Character name tracking
        private string? _currentCharacterName;
        private string? _currentSystemName;
        private string? _currentSystemSecurity;

        public EveLogParser()
        {
        }

        /// <summary>
        /// Parse a single line from an EVE combat log file
        /// </summary>
        /// <param name="logLine">The log line to parse</param>
        /// <param name="characterName">The current character name (for context)</param>
        /// <returns>RepairEvent if the line contains repair data, null otherwise</returns>
        public RepairEvent? ParseLogLine(string logLine, string characterName)
        {
            if (string.IsNullOrWhiteSpace(logLine))
                return null;

            _currentCharacterName = characterName;

            // Check for system information updates
            var systemMatch = SystemInfoPattern.Match(logLine);
            if (systemMatch.Success)
            {
                _currentSystemName = systemMatch.Groups["system"].Value;
                _currentSystemSecurity = systemMatch.Groups["security"].Value;
                return null; // System info doesn't create repair events
            }

            // Try to parse different types of repair events
            RepairEvent? repairEvent = null;

            // Check for outgoing repairs (you repping others)
            var outgoingMatch = RepairLogPattern.Match(logLine);
            if (outgoingMatch.Success && outgoingMatch.Groups["direction"].Value.Contains("remotely repairs"))
            {
                repairEvent = CreateRepairEvent(outgoingMatch, "Outgoing");
            }

            // Check for incoming repairs (others repping you)
            if (repairEvent == null)
            {
                var incomingMatch = IncomingRepairPattern.Match(logLine);
                if (incomingMatch.Success)
                {
                    repairEvent = CreateRepairEvent(incomingMatch, "Incoming");
                }
            }

            // Check for self-repairs
            if (repairEvent == null)
            {
                var selfMatch = SelfRepairPattern.Match(logLine);
                if (selfMatch.Success)
                {
                    repairEvent = CreateSelfRepairEvent(selfMatch);
                }
            }

            return repairEvent;
        }

        /// <summary>
        /// Parse multiple log lines and return all repair events found
        /// </summary>
        /// <param name="logLines">Array of log lines</param>
        /// <param name="characterName">Current character name</param>
        /// <returns>List of repair events</returns>
        public List<RepairEvent> ParseLogLines(string[] logLines, string characterName)
        {
            var events = new List<RepairEvent>();

            foreach (var line in logLines)
            {
                var repairEvent = ParseLogLine(line, characterName);
                if (repairEvent != null)
                {
                    events.Add(repairEvent);
                }
            }

            return events;
        }

        /// <summary>
        /// Create a RepairEvent from a regex match for outgoing/incoming repairs
        /// </summary>
        private RepairEvent CreateRepairEvent(Match match, string direction)
        {
            var timestamp = ParseEveTimestamp(match.Groups["timestamp"].Value);
            var repairType = CapitalizeFirst(match.Groups["repairtype"].Value);
            var amount = int.Parse(match.Groups["amount"].Value);

            var repairEvent = new RepairEvent
            {
                Timestamp = timestamp,
                RepairType = repairType,
                Amount = amount,
                Direction = direction,
                SystemName = _currentSystemName ?? "Unknown",
                SystemSecurity = _currentSystemSecurity ?? "Unknown",
                IskValue = CalculateRepairValue(amount, repairType)
            };

            if (direction == "Outgoing")
            {
                // You are repairing someone else
                repairEvent.TargetName = match.Groups["targetname"].Value;
                repairEvent.LogiPilot = _currentCharacterName ?? "Unknown";
                repairEvent.RepairModule = match.Groups["module"].Value;
            }
            else
            {
                // Someone else is repairing you
                repairEvent.TargetName = _currentCharacterName ?? "Unknown";
                repairEvent.LogiPilot = match.Groups["logipilot"].Value;
                repairEvent.RepairModule = match.Groups["module"].Value;
            }

            return repairEvent;
        }

        /// <summary>
        /// Create a RepairEvent for self-repairs
        /// </summary>
        private RepairEvent CreateSelfRepairEvent(Match match)
        {
            var timestamp = ParseEveTimestamp(match.Groups["timestamp"].Value);
            var repairType = CapitalizeFirst(match.Groups["repairtype"].Value);
            var amount = int.Parse(match.Groups["amount"].Value);

            return new RepairEvent
            {
                Timestamp = timestamp,
                TargetName = _currentCharacterName ?? "Unknown",
                RepairType = repairType,
                Amount = amount,
                LogiPilot = _currentCharacterName ?? "Unknown",
                RepairModule = match.Groups["module"].Value,
                Direction = "Self",
                SystemName = _currentSystemName ?? "Unknown",
                SystemSecurity = _currentSystemSecurity ?? "Unknown",
                IskValue = CalculateRepairValue(amount, repairType)
            };
        }

        /// <summary>
        /// Parse EVE Online timestamp format (YYYY.MM.DD HH:MM:SS)
        /// </summary>
        private DateTime ParseEveTimestamp(string timestamp)
        {
            try
            {
                // EVE uses format: 2024.01.15 14:30:25
                return DateTime.ParseExact(timestamp, "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                // If parsing fails, use current time
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Capitalize the first letter of a string
        /// </summary>
        private string CapitalizeFirst(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        /// <summary>
        /// Calculate estimated ISK value of a repair
        /// This is a simplified calculation - you can make it more sophisticated later
        /// </summary>
        private decimal CalculateRepairValue(int amount, string repairType)
        {
            // Simple ISK calculation: roughly 0.1 ISK per HP repaired
            // Shield repairs might be slightly cheaper than armor
            decimal baseRate = repairType.ToLower() == "shield" ? 0.08m : 0.12m;
            return Math.Round(amount * baseRate, 2);
        }

        /// <summary>
        /// Get the default EVE log directory for the current user
        /// </summary>
        public static string GetDefaultEveLogDirectory()
        {
            // Standard EVE Online log directory location
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "EVE", "logs", "Gamelogs");
        }

        /// <summary>
        /// Find the most recent combat log file for a character
        /// </summary>
        /// <param name="characterName">Name of the character</param>
        /// <param name="logDirectory">Directory to search (optional, uses default if null)</param>
        /// <returns>Path to the most recent log file, or null if not found</returns>
        public static string? FindLatestLogFile(string characterName, string? logDirectory = null)
        {
            try
            {
                logDirectory ??= GetDefaultEveLogDirectory();

                if (!Directory.Exists(logDirectory))
                    return null;

                // EVE log files are named like: "20240115_143025_<CharacterName>_<SystemName>.txt"
                var logFiles = Directory.GetFiles(logDirectory, $"*_{characterName}_*.txt")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToArray();

                return logFiles.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Read and parse the latest entries from a log file
        /// </summary>
        /// <param name="logFilePath">Path to the log file</param>
        /// <param name="characterName">Character name for context</param>
        /// <param name="maxLines">Maximum number of lines to read from the end</param>
        /// <returns>List of repair events found</returns>
        public List<RepairEvent> ParseLogFile(string logFilePath, string characterName, int maxLines = 100)
        {
            try
            {
                if (!File.Exists(logFilePath))
                    return new List<RepairEvent>();

                // Read the last N lines of the file (most recent entries)
                var lines = ReadLastLines(logFilePath, maxLines);
                return ParseLogLines(lines, characterName);
            }
            catch
            {
                return new List<RepairEvent>();
            }
        }

        /// <summary>
        /// Read the last N lines from a file efficiently
        /// </summary>
        private string[] ReadLastLines(string filePath, int lineCount)
        {
            var lines = new List<string>();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                var allLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    allLines.Add(line);
                }

                // Return the last N lines
                var startIndex = Math.Max(0, allLines.Count - lineCount);
                return allLines.Skip(startIndex).ToArray();
            }
        }
    }
}