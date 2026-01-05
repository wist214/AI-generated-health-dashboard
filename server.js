require('dotenv').config();
const express = require('express');
const cors = require('cors');
const { exec } = require('child_process');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());
app.use(express.static('public'));

// Ensure data directory exists
const dataDir = path.join(__dirname, 'data');
if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
}

const DATA_FILE = path.join(dataDir, 'picooc_data.json');
const DOCKER_IMAGE = 'alexxit/smartscaleconnect:latest';

// Check if Docker is available
function checkDocker() {
    try {
        require('child_process').execSync('docker --version', { stdio: 'pipe' });
        return true;
    } catch (e) {
        return false;
    }
}

// Check if SmartScaleConnect binary exists
function getScaleConnectPath() {
    const possiblePaths = [
        path.join(__dirname, 'scaleconnect.exe'),
        path.join(__dirname, 'scaleconnect'),
        path.join(__dirname, 'scaleconnect_windows_amd64.exe'),
        'scaleconnect'
    ];
    
    // Also check for any SmartScaleConnect-* folders with exe inside
    try {
        const files = fs.readdirSync(__dirname);
        for (const file of files) {
            if (file.startsWith('SmartScaleConnect') || file.startsWith('scaleconnect')) {
                const fullPath = path.join(__dirname, file);
                if (fs.statSync(fullPath).isDirectory()) {
                    // Check for exe in the folder
                    const exeInFolder = path.join(fullPath, 'scaleconnect.exe');
                    const exeInFolder2 = path.join(fullPath, 'scaleconnect_windows_amd64.exe');
                    if (fs.existsSync(exeInFolder)) possiblePaths.unshift(exeInFolder);
                    if (fs.existsSync(exeInFolder2)) possiblePaths.unshift(exeInFolder2);
                } else if (file.endsWith('.exe')) {
                    possiblePaths.unshift(fullPath);
                }
            }
        }
    } catch (e) {
        console.error('Error scanning for binary:', e);
    }
    
    for (const p of possiblePaths) {
        if (fs.existsSync(p)) {
            return p;
        }
    }
    return null;
}

// Sync data from Picooc using Docker
async function syncPicoocDataDocker() {
    const username = process.env.PICOOC_USERNAME;
    const password = process.env.PICOOC_PASSWORD;
    const user = process.env.PICOOC_USER;
    
    if (!username || !password) {
        throw new Error('Picooc credentials not configured. Set PICOOC_USERNAME and PICOOC_PASSWORD in .env file');
    }
    
    // Create YAML config file for Docker
    const fromConfig = user 
        ? `picooc ${username} ${password} ${user}`
        : `picooc ${username} ${password}`;
    
    const yamlContent = `sync_picooc:
  from: "${fromConfig}"
  to: "json stdout"
`;
    
    const configFile = path.join(__dirname, 'scaleconnect.yaml');
    fs.writeFileSync(configFile, yamlContent);
    
    // Convert Windows path to Docker-compatible path
    const dockerPath = __dirname.replace(/\\/g, '/').replace(/^([A-Z]):/, (_, letter) => '/' + letter.toLowerCase());
    
    return new Promise((resolve, reject) => {
        const cmd = `docker run --rm -v "${dockerPath}/scaleconnect.yaml:/config/scaleconnect.yaml" ${DOCKER_IMAGE} scaleconnect -c /config/scaleconnect.yaml`;
        
        console.log('Running sync via Docker...');
        
        exec(cmd, { maxBuffer: 10 * 1024 * 1024 }, (error, stdout, stderr) => {
            if (error && !stdout) {
                console.error('Sync error:', stderr);
                reject(new Error(`Sync failed: ${stderr || error.message}`));
                return;
            }
            
            try {
                const lines = stdout.trim().split('\n');
                let jsonLine = '';
                
                // Find the JSON array in the output
                for (let i = lines.length - 1; i >= 0; i--) {
                    const line = lines[i].trim();
                    if (line.startsWith('[') || line.startsWith('{')) {
                        jsonLine = line;
                        break;
                    }
                }
                
                if (!jsonLine) {
                    throw new Error('No JSON found in output');
                }
                
                const data = JSON.parse(jsonLine);
                resolve(Array.isArray(data) ? data : [data]);
            } catch (e) {
                console.error('Parse error:', e, 'Output:', stdout);
                reject(new Error('Failed to parse sync output: ' + stdout.substring(0, 500)));
            }
        });
    });
}

// Sync data from Picooc using local binary
async function syncPicoocDataBinary() {
    const username = process.env.PICOOC_USERNAME;
    const password = process.env.PICOOC_PASSWORD;
    const user = process.env.PICOOC_USER;
    
    if (!username || !password) {
        throw new Error('Picooc credentials not configured. Set PICOOC_USERNAME and PICOOC_PASSWORD in .env file');
    }
    
    const scaleConnectPath = getScaleConnectPath();
    if (!scaleConnectPath) {
        throw new Error('SmartScaleConnect binary not found');
    }
    
    const fromConfig = user 
        ? `picooc ${username} ${password} ${user}`
        : `picooc ${username} ${password}`;
    
    const config = {
        sync_picooc: {
            from: fromConfig,
            to: 'json stdout'
        }
    };
    
    return new Promise((resolve, reject) => {
        const configJson = JSON.stringify(config);
        const cmd = `"${scaleConnectPath}" -c "${configJson.replace(/"/g, '\\"')}"`;
        
        exec(cmd, { maxBuffer: 10 * 1024 * 1024 }, (error, stdout, stderr) => {
            if (error && !stdout) {
                console.error('Sync error:', stderr);
                reject(new Error(`Sync failed: ${stderr || error.message}`));
                return;
            }
            
            try {
                const data = JSON.parse(stdout.trim());
                resolve(Array.isArray(data) ? data : [data]);
            } catch (e) {
                console.error('Parse error:', e, 'Output:', stdout);
                reject(new Error('Failed to parse sync output'));
            }
        });
    });
}

// Main sync function - tries binary first, then Docker
async function syncPicoocData() {
    const binaryPath = getScaleConnectPath();
    
    if (binaryPath) {
        console.log('Using local binary:', binaryPath);
        return syncPicoocDataBinary();
    } else if (checkDocker()) {
        console.log('Using Docker container');
        return syncPicoocDataDocker();
    } else {
        throw new Error('Neither SmartScaleConnect binary nor Docker is available');
    }
}

// Load cached data
function loadCachedData() {
    try {
        if (fs.existsSync(DATA_FILE)) {
            const content = fs.readFileSync(DATA_FILE, 'utf-8');
            return JSON.parse(content);
        }
    } catch (e) {
        console.error('Failed to load cached data:', e);
    }
    return [];
}

// Save data to cache
function saveDataToCache(data) {
    try {
        fs.writeFileSync(DATA_FILE, JSON.stringify(data, null, 2));
    } catch (e) {
        console.error('Failed to save data:', e);
    }
}

// Merge new data with existing, avoiding duplicates
function mergeData(existing, newData) {
    const dataMap = new Map();
    
    // Add existing data
    for (const item of existing) {
        const key = item.Date || item.date;
        if (key) {
            dataMap.set(key, item);
        }
    }
    
    // Add/update with new data
    for (const item of newData) {
        const key = item.Date || item.date;
        if (key) {
            dataMap.set(key, item);
        }
    }
    
    // Convert back to array and sort by date
    const merged = Array.from(dataMap.values());
    merged.sort((a, b) => {
        const dateA = new Date(a.Date || a.date);
        const dateB = new Date(b.Date || b.date);
        return dateB - dateA;
    });
    
    return merged;
}

// API Routes

// Get all health data
app.get('/api/data', (req, res) => {
    const data = loadCachedData();
    res.json(data);
});

// Sync data from Picooc
app.post('/api/sync', async (req, res) => {
    try {
        const newData = await syncPicoocData();
        const existingData = loadCachedData();
        const mergedData = mergeData(existingData, newData);
        saveDataToCache(mergedData);
        res.json({ success: true, count: mergedData.length, data: mergedData });
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

// Get latest measurement
app.get('/api/latest', (req, res) => {
    const data = loadCachedData();
    if (data.length > 0) {
        res.json(data[0]);
    } else {
        res.json(null);
    }
});

// Get statistics
app.get('/api/stats', (req, res) => {
    const data = loadCachedData();
    
    if (data.length === 0) {
        return res.json({ message: 'No data available' });
    }
    
    const weights = data.filter(d => d.Weight > 0).map(d => d.Weight);
    const bodyFats = data.filter(d => d.BodyFat > 0).map(d => d.BodyFat);
    const bmis = data.filter(d => d.BMI > 0).map(d => d.BMI);
    
    const stats = {
        totalMeasurements: data.length,
        weight: weights.length > 0 ? {
            current: weights[0],
            min: Math.min(...weights),
            max: Math.max(...weights),
            avg: (weights.reduce((a, b) => a + b, 0) / weights.length).toFixed(2)
        } : null,
        bodyFat: bodyFats.length > 0 ? {
            current: bodyFats[0],
            min: Math.min(...bodyFats),
            max: Math.max(...bodyFats),
            avg: (bodyFats.reduce((a, b) => a + b, 0) / bodyFats.length).toFixed(2)
        } : null,
        bmi: bmis.length > 0 ? {
            current: bmis[0],
            min: Math.min(...bmis),
            max: Math.max(...bmis),
            avg: (bmis.reduce((a, b) => a + b, 0) / bmis.length).toFixed(2)
        } : null,
        firstMeasurement: data[data.length - 1]?.Date,
        lastMeasurement: data[0]?.Date
    };
    
    res.json(stats);
});

// Check configuration status
app.get('/api/status', (req, res) => {
    const hasCredentials = !!(process.env.PICOOC_USERNAME && process.env.PICOOC_PASSWORD);
    const hasBinary = !!getScaleConnectPath();
    const hasDocker = checkDocker();
    const dataCount = loadCachedData().length;
    
    res.json({
        configured: hasCredentials && (hasBinary || hasDocker),
        hasCredentials,
        hasBinary,
        hasDocker,
        binaryPath: getScaleConnectPath(),
        dataCount,
        syncMethod: hasBinary ? 'binary' : (hasDocker ? 'docker' : 'none')
    });
});

// Serve the main page
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, () => {
    console.log(`Health Aggregator running at http://localhost:${PORT}`);
    console.log('');
    
    const hasCredentials = !!(process.env.PICOOC_USERNAME && process.env.PICOOC_PASSWORD);
    const hasBinary = !!getScaleConnectPath();
    const hasDocker = checkDocker();
    
    if (!hasCredentials) {
        console.log('⚠️  Picooc credentials not configured. Create a .env file with:');
        console.log('   PICOOC_USERNAME=your_email');
        console.log('   PICOOC_PASSWORD=your_password');
        console.log('');
    }
    
    if (hasBinary) {
        console.log('✅ SmartScaleConnect binary found:', getScaleConnectPath());
    } else if (hasDocker) {
        console.log('✅ Docker available - will use Docker container for sync');
    } else {
        console.log('⚠️  Neither SmartScaleConnect binary nor Docker found.');
        console.log('');
    }
    
    if (hasCredentials && (hasBinary || hasDocker)) {
        console.log('✅ Ready to sync data from Picooc!');
    }
});
