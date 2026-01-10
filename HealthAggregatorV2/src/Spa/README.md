# Health Aggregator V2 - React SPA

A modern React TypeScript Single-Page Application for the Health Aggregator dashboard.

## Technology Stack

- **React 18** - UI library
- **TypeScript 5** - Type safety
- **Vite 5** - Build tool with HMR
- **Fluent UI React v9** - Microsoft's design system (customized)
- **TanStack Query** - Server state management
- **Chart.js** - Data visualization
- **Axios** - HTTP client
- **CSS Modules** - Component styling

## Project Structure

```
src/
├── main.tsx              # Application entry point
├── App.tsx               # Root component
├── features/             # Feature modules
│   ├── dashboard/        # Dashboard feature
│   │   ├── components/   # Feature components
│   │   ├── hooks/        # Custom hooks
│   │   ├── services/     # API services
│   │   └── types/        # TypeScript types
│   └── placeholder/      # Placeholder pages
├── shared/               # Shared code
│   ├── api/              # API client
│   ├── components/       # Shared components
│   └── utils/            # Utility functions
└── styles/               # Global styles and theme
```

## Getting Started

### Prerequisites

- Node.js 18+
- npm 9+

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The app will be available at `http://localhost:3000`.

### Available Scripts

```bash
# Development
npm run dev        # Start dev server

# Build
npm run build      # Build for production
npm run preview    # Preview production build

# Testing
npm run test       # Run unit tests
npm run test:ui    # Run tests with UI
npm run test:e2e   # Run E2E tests

# Linting
npm run lint       # Run ESLint
```

## Design System

This SPA maintains **pixel-perfect visual parity** with the V1 dashboard. Key design features:

- Dark cyberpunk theme with gradient accents
- Glass morphism card effects
- Custom color palette (cyan #00d4ff, purple #7c3aed)
- Chart.js for data visualization

See [src/styles/design-tokens.css](src/styles/design-tokens.css) for CSS custom properties.

## API Integration

The SPA connects to the Health Aggregator V2 API. Configure the API URL in:

- `.env.development` - Development API URL
- `.env.production` - Production API URL

## Deployment

The SPA is designed for deployment to Azure Static Web Apps.

```bash
# Build for production
npm run build

# Output in 'dist' folder
```
