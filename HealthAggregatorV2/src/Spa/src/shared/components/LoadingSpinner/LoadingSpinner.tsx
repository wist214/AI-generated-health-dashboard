import React from 'react';
import { Spinner } from '@fluentui/react-components';
import styles from './LoadingSpinner.module.css';

interface LoadingSpinnerProps {
  /** Label text to display below the spinner */
  label?: string;
  /** Size of the spinner */
  size?: 'tiny' | 'extra-small' | 'small' | 'medium' | 'large' | 'extra-large' | 'huge';
  /** Whether to display full-page overlay */
  fullPage?: boolean;
}

/**
 * Loading spinner component with optional label
 * Uses Fluent UI Spinner with custom styling
 */
export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  label = 'Loading...',
  size = 'medium',
  fullPage = false,
}) => {
  const containerClass = fullPage ? styles.fullPage : styles.container;

  return (
    <div className={containerClass}>
      <div className={styles.spinnerWrapper}>
        <Spinner
          size={size}
          label={label}
          labelPosition="below"
          className={styles.spinner}
        />
      </div>
    </div>
  );
};
