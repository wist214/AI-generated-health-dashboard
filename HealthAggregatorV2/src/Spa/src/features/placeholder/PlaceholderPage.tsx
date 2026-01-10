import React from 'react';
import styles from './PlaceholderPage.module.css';

interface PlaceholderPageProps {
  title: string;
  icon: string;
  description?: string;
}

/**
 * Placeholder page component for features not yet implemented
 */
export const PlaceholderPage: React.FC<PlaceholderPageProps> = ({
  title,
  icon,
  description = 'This feature is coming soon.',
}) => {
  return (
    <div className={styles.container}>
      <div className={styles.icon}>{icon}</div>
      <h2 className={styles.title}>{title}</h2>
      <p className={styles.description}>{description}</p>
    </div>
  );
};

export default PlaceholderPage;
