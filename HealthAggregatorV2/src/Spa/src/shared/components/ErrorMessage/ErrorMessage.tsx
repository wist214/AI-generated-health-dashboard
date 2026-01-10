import React from 'react';
import styles from './ErrorMessage.module.css';

interface ErrorMessageProps {
  /** Error message to display */
  message: string;
  /** Optional retry callback */
  onRetry?: () => void;
  /** Optional title */
  title?: string;
}

/**
 * Error message component for displaying API errors or other failures
 */
export const ErrorMessage: React.FC<ErrorMessageProps> = ({
  message,
  onRetry,
  title = 'Error',
}) => {
  return (
    <div className={styles.container}>
      <div className={styles.icon}>❌</div>
      <h3 className={styles.title}>{title}</h3>
      <p className={styles.message}>{message}</p>
      {onRetry && (
        <button type="button" className={styles.retryButton} onClick={onRetry}>
          Try Again
        </button>
      )}
    </div>
  );
};
