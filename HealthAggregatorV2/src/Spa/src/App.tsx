import React, { useState } from 'react';
import { FluentProvider } from '@fluentui/react-components';
import { AppLayout } from '@shared/components/Layout';
import { ErrorBoundary } from '@shared/components/ErrorBoundary';
import { DashboardPage } from '@features/dashboard';
import { PlaceholderPage } from '@features/placeholder';
import { healthDarkTheme } from './styles/theme';

/**
 * Main application component
 * Provides theme context and routing via tab navigation
 */
const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState('dashboard');

  const handleSettingsClick = () => {
    // TODO: Implement settings modal
    console.log('Settings clicked');
  };

  const renderTabContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return <DashboardPage />;
      case 'weight':
        return (
          <PlaceholderPage
            title="Weight Tracking"
            icon="⚖️"
            description="Track your weight, body fat, BMI, and muscle mass from Picooc smart scale."
          />
        );
      case 'oura':
        return (
          <PlaceholderPage
            title="Oura Ring Data"
            icon="💍"
            description="View your sleep, readiness, and activity scores from Oura Ring."
          />
        );
      case 'food':
        return (
          <PlaceholderPage
            title="Food & Nutrition"
            icon="🍎"
            description="Track your calories, macros, and micronutrients from Cronometer."
          />
        );
      default:
        return <DashboardPage />;
    }
  };

  return (
    <FluentProvider theme={healthDarkTheme}>
      <ErrorBoundary>
        <AppLayout
          activeTab={activeTab}
          onTabChange={setActiveTab}
          onSettingsClick={handleSettingsClick}
        >
          {renderTabContent()}
        </AppLayout>
      </ErrorBoundary>
    </FluentProvider>
  );
};

export default App;
