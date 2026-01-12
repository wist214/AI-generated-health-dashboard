import React from 'react';
import styles from './Insights.module.css';

interface InsightData {
  latest: {
    weight?: number | null;
    bodyFat?: number | null;
    weightDate?: string | null;
    sleepScore?: number | null;
    readinessScore?: number | null;
    steps?: number | null;
  };
  weekAgo?: {
    weight?: number | null;
    sleepScore?: number | null;
  };
  avgSleepScore?: number | null;
}

interface InsightsProps {
  data: InsightData;
}

/**
 * Dashboard insights section with actionable health tips
 */
export const Insights: React.FC<InsightsProps> = ({ data }) => {
  const insights: Array<{ emoji: string; text: string }> = [];
  const { latest, weekAgo, avgSleepScore } = data;

  // Weight progress insight
  if (latest.weight && weekAgo?.weight) {
    const diff = latest.weight - weekAgo.weight;
    if (diff < -0.5) {
      insights.push({
        emoji: '🎉',
        text: `Great progress! You've lost ${Math.abs(diff).toFixed(1)}kg over the past 7 days.`,
      });
    } else if (diff > 0.5) {
      insights.push({
        emoji: '📊',
        text: `Your weight increased by ${diff.toFixed(1)}kg this week. Stay consistent with your goals!`,
      });
    }
  }

  // Current weight insight
  if (latest.weight && latest.weightDate) {
    const date = new Date(latest.weightDate).toLocaleDateString();
    insights.push({
      emoji: '⚖️',
      text: `Current weight: ${latest.weight.toFixed(1)}kg (measured ${date})`,
    });
  }

  // Sleep score insight
  if (latest.sleepScore !== null && latest.sleepScore !== undefined) {
    if (latest.sleepScore >= 85) {
      insights.push({
        emoji: '😴',
        text: `Excellent sleep score of ${latest.sleepScore}! You're well-rested.`,
      });
    } else if (latest.sleepScore >= 70) {
      insights.push({
        emoji: '😴',
        text: `Good sleep score of ${latest.sleepScore}. Try to maintain consistent sleep times.`,
      });
    } else if (latest.sleepScore > 0) {
      insights.push({
        emoji: '😴',
        text: `Sleep score of ${latest.sleepScore}. Consider going to bed earlier tonight.`,
      });
    }
  }

  // Average sleep score insight
  if (avgSleepScore !== null && avgSleepScore !== undefined && avgSleepScore > 0) {
    insights.push({
      emoji: '📊',
      text: `Your 7-day average sleep score is ${Math.round(avgSleepScore)}.`,
    });
  }

  // Readiness insight
  if (latest.readinessScore !== null && latest.readinessScore !== undefined) {
    if (latest.readinessScore >= 85) {
      insights.push({
        emoji: '⚡',
        text: `High readiness score of ${latest.readinessScore}! Great day for intense activity.`,
      });
    } else if (latest.readinessScore >= 70) {
      insights.push({
        emoji: '⚡',
        text: `Readiness score of ${latest.readinessScore}. Moderate activity is recommended.`,
      });
    } else if (latest.readinessScore > 0) {
      insights.push({
        emoji: '⚡',
        text: `Low readiness of ${latest.readinessScore}. Consider a rest day or light activity.`,
      });
    }
  }

  // Steps insight
  if (latest.steps !== null && latest.steps !== undefined) {
    if (latest.steps >= 10000) {
      insights.push({
        emoji: '🏃',
        text: `Amazing! You've hit ${latest.steps.toLocaleString()} steps today!`,
      });
    } else if (latest.steps >= 5000) {
      insights.push({
        emoji: '👟',
        text: `${latest.steps.toLocaleString()} steps so far. Keep moving to hit 10,000!`,
      });
    } else if (latest.steps > 0) {
      insights.push({
        emoji: '👟',
        text: `Only ${latest.steps.toLocaleString()} steps so far. Try a short walk!`,
      });
    }
  }

  // If no insights, show a default message
  if (insights.length === 0) {
    insights.push({
      emoji: '📊',
      text: 'Sync your devices to see personalized insights about your health.',
    });
  }

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>💡 Insights</h3>
      <div className={styles.insightList}>
        {insights.map((insight, index) => (
          <div key={index} className={styles.insight}>
            <span className={styles.emoji}>{insight.emoji}</span>
            <span className={styles.text}>{insight.text}</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default Insights;
