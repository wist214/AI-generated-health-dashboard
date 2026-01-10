import React, { ReactNode } from 'react';
import { AppHeader } from './AppHeader';
import { TabNavigation, TabConfig } from './TabNavigation';
import styles from './AppLayout.module.css';

const TABS: TabConfig[] = [
  { id: 'dashboard', label: 'Dashboard', icon: '📊', themeClass: 'dashboard' },
  { id: 'weight', label: 'Weight', icon: '⚖️' },
  { id: 'oura', label: 'Oura Ring', icon: '💍', themeClass: 'oura' },
  { id: 'food', label: 'Food', icon: '🍎', themeClass: 'food' },
];

interface AppLayoutProps {
  children: ReactNode;
  activeTab: string;
  onTabChange: (tab: string) => void;
  onSettingsClick?: () => void;
}

/**
 * Main application layout with header and tab navigation
 * Matches the V1 dashboard structure
 */
export const AppLayout: React.FC<AppLayoutProps> = ({
  children,
  activeTab,
  onTabChange,
  onSettingsClick,
}) => {
  return (
    <div className={styles.root}>
      <a href="#main-content" className="skip-link">
        Skip to main content
      </a>
      
      <AppHeader onSettingsClick={onSettingsClick} />
      
      <div className={styles.container}>
        <TabNavigation
          tabs={TABS}
          activeTab={activeTab}
          onTabChange={onTabChange}
        />
        
        <main id="main-content" className={styles.content}>
          {children}
        </main>
      </div>
    </div>
  );
};
