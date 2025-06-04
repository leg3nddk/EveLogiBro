// EVE Logi Bro - Main JavaScript

// Mock data for testing (we'll replace this with real data later)
let mockStats = {
    currentReps: 0,
    repsPerSecond: 0.0,
    shieldReps: 0,
    armorReps: 0
};

let mockTargets = [
    { name: "Waiting for combat data...", reps: 0 }
];

// Update the display with current stats
function updateDisplay() {
    document.getElementById('currentReps').textContent = mockStats.currentReps;
    document.getElementById('repsPerSecond').textContent = mockStats.repsPerSecond.toFixed(1);
    document.getElementById('shieldReps').textContent = mockStats.shieldReps;
    document.getElementById('armorReps').textContent = mockStats.armorReps;
    
    updateTargetsList();
}

// Update the targets list
function updateTargetsList() {
    const targetsList = document.getElementById('targetsList');
    targetsList.innerHTML = '';
    
    mockTargets.forEach(target => {
        const targetDiv = document.createElement('div');
        targetDiv.className = 'target-item';
        targetDiv.innerHTML = `
            <span class="target-name">${target.name}</span>
            <span class="target-reps">${target.reps} reps</span>
        `;
        targetsList.appendChild(targetDiv);
    });
}

// Simulate some activity (we'll remove this when we have real data)
function simulateActivity() {
    // This is just for testing - adds some fake data every few seconds
    setInterval(() => {
        if (mockStats.currentReps < 50) { // Only simulate up to 50 reps
            mockStats.currentReps += Math.floor(Math.random() * 3) + 1;
            mockStats.shieldReps += Math.floor(Math.random() * 2);
            mockStats.armorReps = mockStats.currentReps - mockStats.shieldReps;
            mockStats.repsPerSecond = (Math.random() * 5).toFixed(1);
            
            // Update targets occasionally
            if (Math.random() > 0.7) {
                mockTargets = [
                    { name: "Capsuleer Alpha", reps: Math.floor(Math.random() * 20) + 5 },
                    { name: "Capsuleer Beta", reps: Math.floor(Math.random() * 15) + 2 }
                ];
            }
            
            updateDisplay();
        }
    }, 2000); // Update every 2 seconds
}

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    console.log('EVE Logi Bro initialized');
    updateDisplay();
    
    // Start simulation (remove this when we have real data)
    simulateActivity();
});

// This is where we'll add real-time data fetching later
async function fetchLatestStats() {
    // TODO: Fetch real data from our C# API
    // For now, we're using mock data
}