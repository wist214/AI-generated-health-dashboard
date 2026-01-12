import React, { useState, useEffect } from 'react';
import styles from './SettingsModal.module.css';

interface Settings {
  height: number;
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
}

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const defaultSettings: Settings = {
  height: 175,
  calories: 2000,
  protein: 150,
  carbs: 250,
  fat: 65,
};

const presets = {
  maintenance: { calories: 2000, protein: 150, carbs: 250, fat: 65 },
  weightLoss: { calories: 1600, protein: 180, carbs: 120, fat: 55 },
  muscleGain: { calories: 2500, protein: 200, carbs: 300, fat: 80 },
};

/**
 * Settings modal for configuring health targets
 */
export const SettingsModal: React.FC<SettingsModalProps> = ({ isOpen, onClose }) => {
  const [settings, setSettings] = useState<Settings>(defaultSettings);
  const [isSaving, setIsSaving] = useState(false);

  // Load settings on mount
  useEffect(() => {
    const savedSettings = localStorage.getItem('healthSettings');
    if (savedSettings) {
      try {
        setSettings({ ...defaultSettings, ...JSON.parse(savedSettings) });
      } catch {
        // Use defaults if parse fails
      }
    }
  }, []);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, onClose]);

  const handleChange = (field: keyof Settings, value: string) => {
    const numValue = parseInt(value, 10) || 0;
    setSettings(prev => ({ ...prev, [field]: numValue }));
  };

  const applyPreset = (preset: keyof typeof presets) => {
    setSettings(prev => ({ ...prev, ...presets[preset] }));
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      localStorage.setItem('healthSettings', JSON.stringify(settings));
      // Optionally save to API
      // await fetch('/api/settings', { method: 'POST', body: JSON.stringify(settings) });
      onClose();
    } catch (error) {
      console.error('Failed to save settings:', error);
    } finally {
      setIsSaving(false);
    }
  };

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div className={styles.overlay} onClick={handleOverlayClick}>
      <div className={styles.modal} onClick={e => e.stopPropagation()}>
        {/* Header */}
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h2 className={styles.title}>⚙️ Settings</h2>
            <span className={styles.subtitle}>Configure your health targets</span>
          </div>
          <button
            type="button"
            className={styles.closeBtn}
            onClick={onClose}
            aria-label="Close settings"
          >
            ×
          </button>
        </div>

        {/* Body */}
        <div className={styles.body}>
          {/* Body Measurements */}
          <div className={styles.section}>
            <h3 className={styles.sectionTitle}>📏 Body Measurements</h3>
            <div className={styles.inputGroup}>
              <label className={styles.label}>Height (cm)</label>
              <input
                type="number"
                className={styles.input}
                value={settings.height}
                onChange={e => handleChange('height', e.target.value)}
                min="100"
                max="250"
                step="1"
              />
            </div>
          </div>

          {/* Daily Nutrition Targets */}
          <div className={styles.section}>
            <h3 className={styles.sectionTitle}>🎯 Daily Nutrition Targets</h3>
            <div className={styles.grid}>
              <div className={styles.inputGroup}>
                <label className={styles.label}>Calories (kcal)</label>
                <input
                  type="number"
                  className={styles.input}
                  value={settings.calories}
                  onChange={e => handleChange('calories', e.target.value)}
                  min="1000"
                  max="5000"
                  step="50"
                />
              </div>
              <div className={styles.inputGroup}>
                <label className={styles.label}>Protein (g)</label>
                <input
                  type="number"
                  className={styles.input}
                  value={settings.protein}
                  onChange={e => handleChange('protein', e.target.value)}
                  min="30"
                  max="300"
                  step="1"
                />
              </div>
              <div className={styles.inputGroup}>
                <label className={styles.label}>Carbs (g)</label>
                <input
                  type="number"
                  className={styles.input}
                  value={settings.carbs}
                  onChange={e => handleChange('carbs', e.target.value)}
                  min="50"
                  max="500"
                  step="1"
                />
              </div>
              <div className={styles.inputGroup}>
                <label className={styles.label}>Fat (g)</label>
                <input
                  type="number"
                  className={styles.input}
                  value={settings.fat}
                  onChange={e => handleChange('fat', e.target.value)}
                  min="20"
                  max="200"
                  step="1"
                />
              </div>
            </div>
          </div>

          {/* Quick Presets */}
          <div className={styles.section}>
            <h3 className={styles.sectionTitle}>⚡ Quick Presets</h3>
            <div className={styles.presets}>
              <button
                type="button"
                className={styles.presetBtn}
                onClick={() => applyPreset('maintenance')}
              >
                Maintenance
              </button>
              <button
                type="button"
                className={styles.presetBtn}
                onClick={() => applyPreset('weightLoss')}
              >
                Weight Loss
              </button>
              <button
                type="button"
                className={styles.presetBtn}
                onClick={() => applyPreset('muscleGain')}
              >
                Muscle Gain
              </button>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className={styles.footer}>
          <button
            type="button"
            className={styles.saveBtn}
            onClick={handleSave}
            disabled={isSaving}
          >
            💾 Save Settings
          </button>
          <button
            type="button"
            className={styles.cancelBtn}
            onClick={onClose}
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};

export default SettingsModal;
