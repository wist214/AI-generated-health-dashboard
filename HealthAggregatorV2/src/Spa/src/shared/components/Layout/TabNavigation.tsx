import React from 'react';
import styles from './TabNavigation.module.css';

export interface TabConfig {
  id: string;
  label: string;
  icon: string;
  themeClass?: 'dashboard' | 'oura' | 'food';
}

interface TabNavigationProps {
  tabs: TabConfig[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
}

/**
 * Tab navigation component matching V1 dashboard design
 * Supports gradient active states per tab theme
 */
export const TabNavigation: React.FC<TabNavigationProps> = ({
  tabs,
  activeTab,
  onTabChange,
}) => {
  return (
    <div className={styles.tabNav} role="tablist" aria-label="Health data sections">
      {tabs.map((tab) => {
        const isActive = activeTab === tab.id;
        const themeClass = tab.themeClass ? styles[tab.themeClass] : '';
        
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            aria-controls={`${tab.id}-tab`}
            className={`${styles.tabBtn} ${themeClass} ${isActive ? styles.active : ''}`}
            onClick={() => onTabChange(tab.id)}
          >
            <span className={styles.tabIcon}>{tab.icon}</span>
            <span className={styles.tabText}>{tab.label}</span>
          </button>
        );
      })}
    </div>
  );
};
