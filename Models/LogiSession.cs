namespace EveLogiBro.Models
{
    public class LogiSession
    {
        // Unique identifier for this combat session
        public int Id { get; set; }
        
        // When this combat session started
        public DateTime StartTime { get; set; }
        
        // When this combat session ended (null if still active)
        public DateTime? EndTime { get; set; }
        
        // Solar system where this session took place
        public string SystemName { get; set; } = string.Empty;
        
        // Security level of the system
        public string SystemSecurity { get; set; } = string.Empty;
        
        // Region where this session took place
        public string RegionName { get; set; } = string.Empty;
        
        // Your ship type during this session
        public string YourShipType { get; set; } = string.Empty;
        
        // Total outgoing repairs performed in this session
        public int TotalOutgoingReps { get; set; }
        
        // Total incoming repairs received in this session
        public int TotalIncomingReps { get; set; }
        
        // Total shield repairs (outgoing + incoming)
        public int TotalShieldReps { get; set; }
        
        // Total armor repairs (outgoing + incoming)
        public int TotalArmorReps { get; set; }
        
        // Total ISK value of all repairs in this session
        public decimal TotalIskValue { get; set; }
        
        // Average repairs per second during this session
        public double AverageRepsPerSecond { get; set; }
        
        // Peak repairs per second achieved in this session
        public double PeakRepsPerSecond { get; set; }
        
        // Whether this session is currently active
        public bool IsActive { get; set; }
        
        // Type of engagement (e.g. "Fleet Fight", "Small Gang", "Solo", "PvE")
        public string EngagementType { get; set; } = string.Empty;
        
        // Collection of all repair events in this session
        public List<RepairEvent> RepairEvents { get; set; } = new List<RepairEvent>();
    }
}