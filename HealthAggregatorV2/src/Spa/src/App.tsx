import React, { useState } from 'react';
import { FluentProvider } from '@fluentui/react-components';
import { AppLayout } from '@shared/components/Layout';
import { ErrorBoundary } from '@shared/components/ErrorBoundary';
import { DashboardPage } from '@features/dashboard';
import { WeightPage } from '@features/weight';
import { OuraPage } from '@features/oura';
import { FoodPage } from '@features/food';
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
        return <WeightPage />;
      case 'oura':
        return <OuraPage />;
      case 'food':
        return <FoodPage />;
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
