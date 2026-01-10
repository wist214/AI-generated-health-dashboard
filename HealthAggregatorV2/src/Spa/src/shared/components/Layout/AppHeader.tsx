import React from 'react';
import styles from './AppHeader.module.css';

interface AppHeaderProps {
  onSettingsClick?: () => void;
}

/**
 * Application header with title and settings button
 * Matches the V1 dashboard header design
 */
export const AppHeader: React.FC<AppHeaderProps> = ({ onSettingsClick }) => {
  return (
    <header className={styles.header}>
      <div className={styles.headerContent}>
        <div className={styles.spacer} />
        <div className={styles.headerText}>
          <h1 className={styles.title}>Health Aggregator</h1>
          <p className={styles.subtitle}>Track your health data from multiple sources</p>
        </div>
        <button
          type="button"
          className={styles.settingsBtn}
          onClick={onSettingsClick}
          aria-label="Open Settings"
        >
          ⚙️
        </button>
      </div>
    </header>
  );
};
