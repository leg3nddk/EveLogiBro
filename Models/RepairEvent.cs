namespace EveLogiBro.Models
{
    public class RepairEvent
    {
        // Unique identifier for this repair event in the database
        public int Id { get; set; }
        
        // When this repair happened (from EVE combat logs)
        public DateTime Timestamp { get; set; }
        
        // Name of the pilot who received the repair
        public string TargetName { get; set; } = string.Empty;
        
        // Corporation name of the target pilot
        public string TargetCorporation { get; set; } = string.Empty;
        
        // Alliance name of the target pilot (can be empty if not in alliance)
        public string TargetAlliance { get; set; } = string.Empty;
        
        // Ship type of the target (e.g. "Raven", "Drake", "Caracal")
        public string TargetShipType { get; set; } = string.Empty;
        
        // Type of repair: "Shield" or "Armor"
        public string RepairType { get; set; } = string.Empty;
        
        // Amount of HP repaired
        public int Amount { get; set; }
        
        // Name of the pilot who performed the repair (you, or someone repping you)
        public string LogiPilot { get; set; } = string.Empty;
        
        // Corporation of the logi pilot
        public string LogiCorporation { get; set; } = string.Empty;
        
        // Alliance of the logi pilot
        public string LogiAlliance { get; set; } = string.Empty;
        
        // Ship type of the logi pilot (e.g. "Basilisk", "Guardian", "Scythe")
        public string LogiShipType { get; set; } = string.Empty;
        
        // Name of the repair module used (e.g. "Large Shield Booster II", "Medium Armor Repairer II")
        public string RepairModule { get; set; } = string.Empty;
        
        // Solar system where this repair took place
        public string SystemName { get; set; } = string.Empty;
        
        // Security level of the system (e.g. "0.5", "-0.3", "0.0")
        public string SystemSecurity { get; set; } = string.Empty;
        
        // Estimated ISK value of this repair (based on repair amount and current prices)
        public decimal IskValue { get; set; }
        
        // Links this repair to a specific combat session/fight
        public int SessionId { get; set; }
        
        // Direction of the repair: "Outgoing" (you repping others) or "Incoming" (others repping you)
        public string Direction { get; set; } = string.Empty;
        
        // Distance to target when repair was applied (in meters)
        public double? DistanceToTarget { get; set; }
    }
}