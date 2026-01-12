/**
 * Stress Modal - Detailed stress data view with charts
 */

// Stress Modal State
const stressModal = {
    chart: null,
    data: [],
    isOpen: false,
    chartPeriod: 30 // Default to 30 days
};

/**
 * Initialize stress modal functionality
 */
function initStressModal() {
    // Create modal HTML if it doesn't exist
    if (!document.getElementById('stressModal')) {
        createStressModalHTML();
    }
}

/**
 * Create the stress modal HTML structure
 */
function createStressModalHTML() {
    const modalHTML = `
        <div id="stressModal" class="detail-modal" style="display: none;">
            <div class="detail-modal-content">
                <div class="detail-modal-header">
                    <h2>🧘 Daily Stress Analysis</h2>
                    <button class="detail-modal-close" onclick="closeStressModal()">&times;</button>
                </div>
                <div class="detail-modal-body">
                    <!-- Summary Stats -->
                    <div class="detail-stats-grid">
                        <div class="detail-stat-card">
                            <h4>Today's Status</h4>
                            <div class="detail-stat-value" id="stressModalStatus">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>Stress Time</h4>
                            <div class="detail-stat-value" id="stressModalStressTime">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>Recovery Time</h4>
                            <div class="detail-stat-value" id="stressModalRecoveryTime">--</div>
                        </div>
                        <div class="detail-stat-card">
                            <h4>30-Day Avg Stress</h4>
                            <div class="detail-stat-value" id="stressModalAvgStress">--</div>
                        </div>
                    </div>
                    
                    <!-- Chart -->
                    <div class="detail-chart-container">
                        <div class="detail-chart-header">
                            <h3>Stress & Recovery Trend</h3>
                            <div class="time-range-selector" id="stress-time-selector">
                                <button type="button" class="time-btn" data-days="7" onclick="setStressChartPeriod(7)">7 Days</button>
                                <button type="button" class="time-btn active" data-days="30" onclick="setStressChartPeriod(30)">30 Days</button>
                                <button type="button" class="time-btn" data-days="90" onclick="setStressChartPeriod(90)">90 Days</button>
                                <button type="button" class="time-btn" data-days="0" onclick="setStressChartPeriod(0)">All</button>
                            </div>
                        </div>
                        <canvas id="stressDetailChart"></canvas>
                    </div>
                    
                    <!-- Recent History Table -->
                    <div class="detail-table-container">
                        <h3>Recent History</h3>
                        <table class="detail-table">
                            <thead>
                                <tr>
                                    <th>Date</th>
                                    <th>Status</th>
                                    <th>Stress (min)</th>
                                    <th>Recovery (min)</th>
                                </tr>
                            </thead>
                            <tbody id="stressHistoryTable">
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    document.body.insertAdjacentHTML('beforeend', modalHTML);
}

/**
 * Set stress chart period and re-render
 */
function setStressChartPeriod(days) {
    console.log('setStressChartPeriod called with:', days);
    stressModal.chartPeriod = days;
    
    // Update button states
    const buttons = document.querySelectorAll('#stress-time-selector .time-btn');
    buttons.forEach(btn => {
        btn.classList.remove('active');
        if (parseInt(btn.dataset.days) === days) {
            btn.classList.add('active');
        }
    });
    
    // Re-render chart with new period
    console.log('Data length:', stressModal.data.length, 'Period:', stressModal.chartPeriod);
    renderStressChart(stressModal.data);
}

/**
 * Open the stress modal and load data
 */
async function openStressModal() {
    const modal = document.getElementById('stressModal');
    if (!modal) return;
    
    modal.style.display = 'flex';
    stressModal.isOpen = true;
    document.body.style.overflow = 'hidden';
    
    await loadStressDetailData();
}

/**
 * Close the stress modal
 */
function closeStressModal() {
    const modal = document.getElementById('stressModal');
    if (modal) {
        modal.style.display = 'none';
    }
    stressModal.isOpen = false;
    document.body.style.overflow = '';
}

/**
 * Load stress detail data from API
 */
async function loadStressDetailData() {
    try {
        // Use Functions API URL for Azure Static Web Apps, port 7071 for localhost
        const API_BASE = window.location.hostname.includes('azurestaticapps.net') 
            ? 'https://func-healthaggregator.azurewebsites.net' 
            : 'http://localhost:7071';
        
        // Load stress data
        const response = await fetch(`${API_BASE}/api/oura/stress`);
        const stressData = await response.json();
        
        // Also load stats for summary
        const statsResponse = await fetch(`${API_BASE}/api/oura/stats`);
        const stats = await statsResponse.json();
        
        stressModal.data = Array.isArray(stressData) ? stressData : [];
        
        updateStressModalUI(stressModal.data, stats);
        renderStressChart(stressModal.data);
        renderStressHistoryTable(stressModal.data);
        
    } catch (e) {
        console.error('Failed to load stress detail data:', e);
    }
}

/**
 * Update stress modal summary stats
 */
function updateStressModalUI(data, stats) {
    // Today's status
    const statusEl = document.getElementById('stressModalStatus');
    if (stats.stress?.daySummary) {
        const summary = stats.stress.daySummary;
        const formatted = summary === 'restored' ? '✨ Restored' :
                         summary === 'normal' ? '😊 Normal' :
                         summary === 'stressful' ? '😰 Stressful' : summary;
        statusEl.textContent = formatted;
        statusEl.className = 'detail-stat-value';
        if (summary === 'restored') statusEl.classList.add('status-good');
        else if (summary === 'stressful') statusEl.classList.add('status-warning');
    } else {
        statusEl.textContent = '--';
    }
    
    // Stress time (convert from seconds to minutes)
    const stressTimeEl = document.getElementById('stressModalStressTime');
    if (stats.stress?.currentStressHigh != null) {
        stressTimeEl.textContent = `${Math.round(stats.stress.currentStressHigh / 60)} min`;
    } else {
        stressTimeEl.textContent = '--';
    }
    
    // Recovery time
    const recoveryTimeEl = document.getElementById('stressModalRecoveryTime');
    if (stats.stress?.currentRecoveryHigh != null) {
        recoveryTimeEl.textContent = `${Math.round(stats.stress.currentRecoveryHigh / 60)} min`;
    } else {
        recoveryTimeEl.textContent = '--';
    }
    
    // 30-day average stress
    const avgStressEl = document.getElementById('stressModalAvgStress');
    if (stats.stress?.avgStressHigh != null) {
        avgStressEl.textContent = `${Math.round(stats.stress.avgStressHigh / 60)} min`;
    } else {
        avgStressEl.textContent = '--';
    }
}

/**
 * Render the stress chart
 */
function renderStressChart(data) {
    const canvas = document.getElementById('stressDetailChart');
    if (!canvas) return;
    
    // Destroy existing chart
    if (stressModal.chart) {
        stressModal.chart.destroy();
    }
    
    // Sort data by date
    let sortedData = [...data].sort((a, b) => a.day.localeCompare(b.day));
    
    // Filter by selected period (actual date range)
    if (stressModal.chartPeriod > 0) {
        const cutoffDate = new Date();
        cutoffDate.setDate(cutoffDate.getDate() - stressModal.chartPeriod);
        const cutoffStr = cutoffDate.toISOString().split('T')[0];
        sortedData = sortedData.filter(d => d.day >= cutoffStr);
    }
    
    console.log('Filtered data count:', sortedData.length, 'from', data.length);
    
    const labels = sortedData.map(d => {
        const date = new Date(d.day);
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    });
    
    const stressMinutes = sortedData.map(d => d.stressHigh != null ? Math.round(d.stressHigh / 60) : null);
    const recoveryMinutes = sortedData.map(d => d.recoveryHigh != null ? Math.round(d.recoveryHigh / 60) : null);
    
    stressModal.chart = new Chart(canvas.getContext('2d'), {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Stress (min)',
                    data: stressMinutes,
                    backgroundColor: 'rgba(239, 68, 68, 0.7)',
                    borderColor: 'rgba(239, 68, 68, 1)',
                    borderWidth: 1
                },
                {
                    label: 'Recovery (min)',
                    data: recoveryMinutes,
                    backgroundColor: 'rgba(34, 197, 94, 0.7)',
                    borderColor: 'rgba(34, 197, 94, 1)',
                    borderWidth: 1
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
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                x: {
                    stacked: false,
                    ticks: { color: '#94a3b8' },
                    grid: { color: 'rgba(148, 163, 184, 0.1)' }
                },
                y: {
                    beginAtZero: true,
                    ticks: { 
                        color: '#94a3b8',
                        callback: value => `${value} min`
                    },
                    grid: { color: 'rgba(148, 163, 184, 0.1)' }
                }
            }
        }
    });
}

/**
 * Render stress history table
 */
function renderStressHistoryTable(data) {
    const tbody = document.getElementById('stressHistoryTable');
    if (!tbody) return;
    
    // Sort by date descending, take last 14 days
    const sortedData = [...data]
        .sort((a, b) => b.day.localeCompare(a.day))
        .slice(0, 14);
    
    tbody.innerHTML = sortedData.map(record => {
        const date = new Date(record.day);
        const dateStr = date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
        
        const status = record.daySummary || '--';
        const statusFormatted = status === 'restored' ? '✨ Restored' :
                               status === 'normal' ? '😊 Normal' :
                               status === 'stressful' ? '😰 Stressful' : status;
        
        const stressMin = record.stressHigh != null ? Math.round(record.stressHigh / 60) : '--';
        const recoveryMin = record.recoveryHigh != null ? Math.round(record.recoveryHigh / 60) : '--';
        
        return `
            <tr>
                <td>${dateStr}</td>
                <td>${statusFormatted}</td>
                <td>${stressMin}</td>
                <td>${recoveryMin}</td>
            </tr>
        `;
    }).join('');
}

// Close modal on escape key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && stressModal.isOpen) {
        closeStressModal();
    }
});

// Close modal on background click
document.addEventListener('click', (e) => {
    if (e.target.id === 'stressModal') {
        closeStressModal();
    }
});

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initStressModal);
} else {
    initStressModal();
}

// Expose functions globally for onclick handlers
window.openStressModal = openStressModal;
window.closeStressModal = closeStressModal;
window.setStressChartPeriod = setStressChartPeriod;
