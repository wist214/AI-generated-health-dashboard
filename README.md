# Health Aggregator - Picooc Data Viewer

A web application to aggregate and visualize your health data from the Picooc smart scale app.

## Features

- 📊 **Real-time Charts** - Visualize weight, body fat, BMI, muscle mass, and more over time
- 📈 **Statistics** - View min, max, and average values for all metrics
- 📋 **Data Table** - Browse all your measurements in a sortable table
- 🔄 **Easy Sync** - One-click sync with your Picooc account
- 🎨 **Modern UI** - Dark theme with responsive design

## Prerequisites

1. **Node.js** (v14 or higher)
2. **SmartScaleConnect** binary - Download from [GitHub Releases](https://github.com/AlexxIT/SmartScaleConnect/releases/)

## Installation

1. Clone or download this project

2. Install dependencies:
   ```bash
   npm install
   ```

3. Download SmartScaleConnect:
   - Go to https://github.com/AlexxIT/SmartScaleConnect/releases/
   - Download the binary for your OS (e.g., `scaleconnect_windows_amd64.exe` for Windows)
   - Rename it to `scaleconnect.exe` (Windows) or `scaleconnect` (Linux/Mac)
   - Place it in the project root directory
   - On Linux/Mac, make it executable: `chmod +x scaleconnect`

4. Create a `.env` file with your Picooc credentials:
   ```env
   PICOOC_USERNAME=your_email@example.com
   PICOOC_PASSWORD=your_password
   PICOOC_USER=optional_specific_user_name
   PORT=3000
   ```

## Usage

1. Start the server:
   ```bash
   npm start
   ```

2. Open your browser to `http://localhost:3000`

3. Click **"Sync Now"** to fetch your data from Picooc

## Configuration

| Environment Variable | Description | Required |
|---------------------|-------------|----------|
| `PICOOC_USERNAME` | Your Picooc account email | Yes |
| `PICOOC_PASSWORD` | Your Picooc account password | Yes |
| `PICOOC_USER` | Specific user name (if multiple users) | No |
| `PORT` | Server port (default: 3000) | No |

## Data Storage

- Your health data is cached locally in `data/picooc_data.json`
- Authorization tokens are stored in `scaleconnect.json` by SmartScaleConnect
- No data is sent to any third-party servers except Picooc's official API

## Supported Metrics

- Weight (kg)
- BMI (Body Mass Index)
- Body Fat (%)
- Body Water (%)
- Bone Mass (kg)
- Muscle Mass (kg)
- Visceral Fat (index)
- Metabolic Age (years)
- Basal Metabolism (kcal)
- And more depending on your scale model

## Troubleshooting

### "SmartScaleConnect binary not found"
- Download the binary from the releases page
- Make sure it's in the project root directory
- Rename it to `scaleconnect.exe` (Windows) or `scaleconnect` (Linux/Mac)

### "Credentials not configured"
- Create a `.env` file in the project root
- Add your Picooc username and password

### Sync fails
- Check your Picooc credentials are correct
- Try logging into the Picooc app on your phone to verify credentials
- Check the console output for detailed error messages

## Credits

- [SmartScaleConnect](https://github.com/AlexxIT/SmartScaleConnect) by AlexxIT - For the amazing scale data synchronization tool
- [Chart.js](https://www.chartjs.org/) - For beautiful charts

## License

MIT
