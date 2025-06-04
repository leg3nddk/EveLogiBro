// EVE Logi Bro - Main JavaScript with Real API Integration

// Global variables to track application state
let isLoading = false;
let updateInterval = null;

// Update the display with current stats from the API
async function updateDisplay() {
    if (isLoading) return; // Prevent multiple simultaneous requests
    
    try {
        isLoading = true;
        
        // Fetch current statistics from our C# API
        const statsResponse = await fetch('/api/repair/stats');
        const stats = await statsResponse.json();
        
        // Update the stats display
        document.getElementById('currentReps').textContent = stats.currentReps;
        document.getElementById('repsPerSecond').textContent = stats.repsPerSecond;
        document.getElementById('shieldReps').textContent = stats.shieldReps;
        document.getElementById('armorReps').textContent = stats.armorReps;
        
        // Fetch current targets from our C# API
        const targetsResponse = await fetch('/api/repair/targets');
        const targets = await targetsResponse.json();
        
        // Update the targets list
        updateTargetsList(targets);
        
        // Update status indicator
        updateConnectionStatus(true);
        
    } catch (error) {
        console.error('Error fetching data:', error);
        updateConnectionStatus(false);
    } finally {
        isLoading = false;
    }
}

// Update the targets list with real data
function updateTargetsList(targets) {
    const targetsList = document.getElementById('targetsList');
    targetsList.innerHTML = '';
    
    targets.forEach(target => {
        const targetDiv = document.createElement('div');
        targetDiv.className = 'target-item';
        
        // Build target info string
        let targetInfo = target.name;
        if (target.corporation && target.corporation !== '') {
            targetInfo += ` [${target.corporation}]`;
        }
        if (target.shipType && target.shipType !== '') {
            targetInfo += ` (${target.shipType})`;
        }
        
        targetDiv.innerHTML = `
            <span class="target-name">${targetInfo}</span>
            <span class="target-reps">${target.reps} HP</span>
        `;
        targetsList.appendChild(targetDiv);
    });
}

// Update connection status indicator
function updateConnectionStatus(connected) {
    let statusDiv = document.getElementById('connectionStatus');
    
    // Create status indicator if it doesn't exist
    if (!statusDiv) {
        statusDiv = document.createElement('div');
        statusDiv.id = 'connectionStatus';
        statusDiv.className = 'connection-status';
        document.querySelector('header').appendChild(statusDiv);
    }
    
    if (connected) {
        statusDiv.textContent = '● Connected to database';
        statusDiv.className = 'connection-status connected';
    } else {
        statusDiv.textContent = '● Connection error';
        statusDiv.className = 'connection-status disconnected';
    }
}

// Start a new logi session
async function startNewSession() {
    try {
        const response = await fetch('/api/repair/session/start', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        });
        
        if (response.ok) {
            console.log('New logi session started');
            // Force immediate update to show the new session
            await updateDisplay();
        } else {
            console.error('Failed to start new session');
        }
    } catch (error) {
        console.error('Error starting new session:', error);
    }
}

// Add a test repair event (for testing purposes)
async function addTestRepairEvent() {
    const testEvent = {
        targetName: "Test Pilot",
        targetCorporation: "Test Corp",
        targetAlliance: "Test Alliance",
        targetShipType: "Drake",
        repairType: "Shield",
        amount: 1500,
        logiPilot: "Your Character",
        logiCorporation: "Your Corp",
        logiAlliance: "Your Alliance",
        logiShipType: "Basilisk",
        repairModule: "Large Shield Booster II",
        systemName: "Jita",
        systemSecurity: "1.0",
        iskValue: 150.50,
        direction: "Outgoing",
        distanceToTarget: 2500.0
    };
    
    try {
        const response = await fetch('/api/repair/event', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(testEvent)
        });
        
        if (response.ok) {
            console.log('Test repair event added');
            // Update display to show the new data
            await updateDisplay();
        } else {
            console.error('Failed to add test event');
        }
    } catch (error) {
        console.error('Error adding test event:', error);
    }
}

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    console.log('EVE Logi Bro initialized - connecting to database');
    
    // Initial display update
    updateDisplay();
    
    // Set up automatic updates every 2 seconds
    updateInterval = setInterval(updateDisplay, 2000);
    
    // Add keyboard shortcuts for testing (remove these in production)
    document.addEventListener('keydown', function(event) {
        // Press 'T' to add a test repair event
        if (event.key.toLowerCase() === 't' && event.altKey) {
            event.preventDefault();
            addTestRepairEvent();
            console.log('Test repair event added (Ctrl+T)');
        }
        
        // Press 'N' to start a new session
        if (event.key.toLowerCase() === 'n' && event.altKey) {
            event.preventDefault();
            startNewSession();
            console.log('New session started (Ctrl+N)');
        }
    });
    
    console.log('Keyboard shortcuts: Ctrl+T = Add test repair, Ctrl+N = New session');
});

// Clean up when page is unloaded
window.addEventListener('beforeunload', function() {
    if (updateInterval) {
        clearInterval(updateInterval);
    }
});

// Utility function to format numbers nicely
function formatNumber(num) {
    if (num >= 1000000) {
        return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
        return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
}