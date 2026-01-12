/**
 * Workouts Modal - Detailed workouts view with charts
 */

// Workouts Modal State
const workoutsModal = {
    chart: null,
    data: [],
    isOpen: false
};

/**
 * Initialize workouts modal functionality
 */
function initWorkoutsModal() {
    // Create modal HTML if it doesn't exist
    if (!document.getElementById('workoutsModal')) {
        createWorkoutsModalHTML();
    }
}

/**
 * Create the workouts modal HTML structure
 */
function createWorkoutsModalHTML() {
    const modalHTML = `
        <div id="workoutsModal" class="detail-modal" style="display: none;">
            <div class="detail-modal-content">
                <div class="detail-modal-header">
                    <h2>🏋️ Workouts Overview</h2>
                    <button class="detail-modal-close" onclick="closeWorkoutsModal()">&times;</button>
                </div>
                <div class="detail-modal-body">
                    <!-- Summary Stats -->
                    <div class="detail-stats-grid">
                        <div class="detail-stat-card">
                            <h4>Total Workouts</h4>
                            <div class="detail-stat-value" id="workoutsModalTotal">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>Total Calories</h4>
                            <div class="detail-stat-value" id="workoutsModalCalories">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>Total Distance</h4>
                            <div class="detail-stat-value" id="workoutsModalDistance">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>This Week</h4>
                            <div class="detail-stat-value" id="workoutsModalWeek">--</div>
                        </div>
                    </div>
                    
                    <!-- Chart -->
                    <div class="detail-chart-container">
                        <h3>Workout Activity (Last 30 Days)</h3>
                        <canvas id="workoutsDetailChart"></canvas>
                    </div>
                    
                    <!-- Workouts List -->
                    <div class="detail-table-container">
                        <h3>Recent Workouts</h3>
                        <div id="workoutsList" class="workouts-list">
                            <p class="no-data">No workouts recorded</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHTML);
}

/**
 * Open the workouts modal and load data
 */
async function openWorkoutsModal() {
    const modal = document.getElementById('workoutsModal');
    if (!modal) return;
    
    modal.style.display = 'flex';
    workoutsModal.isOpen = true;
    document.body.style.overflow = 'hidden';
    
    await loadWorkoutsDetailData();
}

/**
 * Close the workouts modal
 */
function closeWorkoutsModal() {
    const modal = document.getElementById('workoutsModal');
    if (modal) {
        modal.style.display = 'none';
    }
    workoutsModal.isOpen = false;
    document.body.style.overflow = '';
}

/**
 * Load workouts detail data from API
 */
async function loadWorkoutsDetailData() {
    try {
        const API_BASE = window.location.hostname.includes('azurestaticapps.net') 
            ? 'https://func-healthaggregator.azurewebsites.net' 
            : 'http://localhost:7071';
        
        // Load workouts data
        const response = await fetch(`${API_BASE}/api/oura/workouts`);
        const workoutsData = await response.json();
        
        // Also load stats for summary
        const statsResponse = await fetch(`${API_BASE}/api/oura/stats`);
        const stats = await statsResponse.json();
        
        workoutsModal.data = Array.isArray(workoutsData) ? workoutsData : [];
        
        updateWorkoutsModalUI(workoutsModal.data, stats);
        renderWorkoutsChart(workoutsModal.data);
        renderWorkoutsList(workoutsModal.data);
        
    } catch (e) {
        console.error('Failed to load workouts detail data:', e);
    }
}

/**
 * Update workouts modal summary stats
 */
function updateWorkoutsModalUI(data, stats) {
    // Total workouts
    const totalEl = document.getElementById('workoutsModalTotal');
    totalEl.textContent = data.length || '0';
    
    // Total calories
    const caloriesEl = document.getElementById('workoutsModalCalories');
    const totalCalories = data.reduce((sum, w) => sum + (w.calories || 0), 0);
    caloriesEl.textContent = totalCalories > 0 ? `${totalCalories.toLocaleString()} kcal` : '--';
    
    // Total distance
    const distanceEl = document.getElementById('workoutsModalDistance');
    const totalDistance = data.reduce((sum, w) => sum + (w.distance || 0), 0);
    if (totalDistance > 0) {
        distanceEl.textContent = `${(totalDistance / 1000).toFixed(1)} km`;
    } else {
        distanceEl.textContent = '--';
    }
    
    // This week
    const weekEl = document.getElementById('workoutsModalWeek');
    if (stats.workouts?.recentCount != null) {
        weekEl.textContent = stats.workouts.recentCount;
    } else {
        weekEl.textContent = '0';
    }
}

/**
 * Render the workouts chart
 */
function renderWorkoutsChart(data) {
    const canvas = document.getElementById('workoutsDetailChart');
    if (!canvas) return;
    
    // Destroy existing chart
    if (workoutsModal.chart) {
        workoutsModal.chart.destroy();
    }
    
    // Group workouts by day for the last 30 days
    const today = new Date();
    const thirtyDaysAgo = new Date(today);
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
    
    const dailyData = {};
    
    // Initialize all days with 0
    for (let d = new Date(thirtyDaysAgo); d <= today; d.setDate(d.getDate() + 1)) {
        const dateStr = d.toISOString().split('T')[0];
        dailyData[dateStr] = { count: 0, calories: 0, duration: 0 };
    }
    
    // Aggregate workout data
    data.forEach(workout => {
        const day = workout.day || (workout.startDatetime ? workout.startDatetime.split('T')[0] : null);
        if (day && dailyData[day]) {
            dailyData[day].count++;
            dailyData[day].calories += workout.calories || 0;
            dailyData[day].duration += workout.totalDuration || 0;
        }
    });
    
    const sortedDays = Object.keys(dailyData).sort();
    const labels = sortedDays.map(d => {
        const date = new Date(d);
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    });
    
    const caloriesData = sortedDays.map(d => dailyData[d].calories);
    const countData = sortedDays.map(d => dailyData[d].count);
    
    workoutsModal.chart = new Chart(canvas.getContext('2d'), {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Calories Burned',
                    data: caloriesData,
                    backgroundColor: 'rgba(249, 115, 22, 0.7)',
                    borderColor: 'rgba(249, 115, 22, 1)',
                    borderWidth: 1,
                    yAxisID: 'y'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: { color: '#e2e8f0' }
                },
                tooltip: {
                    callbacks: {
                        afterLabel: function(context) {
                            const idx = context.dataIndex;
                            const count = countData[idx];
                            return count > 0 ? `${count} workout(s)` : '';
                        }
                    }
                }
            },
            scales: {
                x: {
                    ticks: { color: '#94a3b8' },
                    grid: { color: 'rgba(148, 163, 184, 0.1)' }
                },
                y: {
                    beginAtZero: true,
                    position: 'left',
                    ticks: { 
                        color: '#94a3b8',
                        callback: value => `${value} kcal`
                    },
                    grid: { color: 'rgba(148, 163, 184, 0.1)' }
                }
            }
        }
    });
}

/**
 * Render workouts list
 */
function renderWorkoutsList(data) {
    const container = document.getElementById('workoutsList');
    if (!container) return;
    
    if (!data || data.length === 0) {
        container.innerHTML = '<p class="no-data">No workouts recorded in the last 30 days</p>';
        return;
    }
    
    // Sort by date descending
    const sortedData = [...data].sort((a, b) => {
        const dateA = a.startDatetime || a.day || '';
        const dateB = b.startDatetime || b.day || '';
        return dateB.localeCompare(dateA);
    });
    
    container.innerHTML = sortedData.map(workout => {
        const dateStr = workout.startDatetime || workout.day;
        const date = dateStr ? new Date(dateStr) : null;
        const formattedDate = date ? date.toLocaleDateString('en-US', { 
            weekday: 'short', 
            month: 'short', 
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }) : 'Unknown date';
        
        const activity = workout.activity || 'Workout';
        const activityIcon = getActivityIcon(activity);
        
        const calories = workout.calories ? `${workout.calories} kcal` : '';
        const distance = workout.distance ? `${(workout.distance / 1000).toFixed(2)} km` : '';
        const duration = workout.totalDuration ? formatDuration(workout.totalDuration) : '';
        const intensity = workout.intensity ? capitalizeFirst(workout.intensity) : '';
        const source = workout.source || '';
        
        const details = [calories, distance, duration, intensity].filter(x => x).join(' • ');
        
        return `
            <div class="workout-item">
                <div class="workout-icon">${activityIcon}</div>
                <div class="workout-info">
                    <div class="workout-activity">${capitalizeFirst(activity)}</div>
                    <div class="workout-date">${formattedDate}</div>
                    <div class="workout-details">${details || 'No details'}</div>
                    ${source ? `<div class="workout-source">Source: ${source}</div>` : ''}
                </div>
            </div>
        `;
    }).join('');
}

/**
 * Get icon for activity type
 */
function getActivityIcon(activity) {
    const icons = {
        'running': '🏃',
        'walking': '🚶',
        'cycling': '🚴',
        'swimming': '🏊',
        'gym': '🏋️',
        'strength_training': '💪',
        'yoga': '🧘',
        'hiit': '🔥',
        'dancing': '💃',
        'hiking': '🥾',
        'tennis': '🎾',
        'basketball': '🏀',
        'soccer': '⚽',
        'golf': '⛳',
        'skiing': '⛷️',
        'snowboarding': '🏂',
        'surfing': '🏄',
        'rowing': '🚣',
        'elliptical': '🏃',
        'stair_climbing': '🪜',
        'other': '🏃'
    };
    
    const lowerActivity = (activity || '').toLowerCase();
    return icons[lowerActivity] || '🏃';
}

/**
 * Format duration in seconds to readable string
 */
function formatDuration(seconds) {
    if (!seconds) return '';
    
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    
    if (hours > 0) {
        return `${hours}h ${minutes}m`;
    }
    return `${minutes}m`;
}

/**
 * Capitalize first letter
 */
function capitalizeFirst(str) {
    if (!str) return '';
    return str.charAt(0).toUpperCase() + str.slice(1).replace(/_/g, ' ');
}

// Close modal on escape key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && workoutsModal.isOpen) {
        closeWorkoutsModal();
    }
});

// Close modal on background click
document.addEventListener('click', (e) => {
    if (e.target.id === 'workoutsModal') {
        closeWorkoutsModal();
    }
});

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initWorkoutsModal);
} else {
    initWorkoutsModal();
}
