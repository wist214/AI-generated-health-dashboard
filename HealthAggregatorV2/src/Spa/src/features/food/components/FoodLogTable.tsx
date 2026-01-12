import React, { useState } from 'react';
import styles from './FoodLogTable.module.css';

export interface FoodServing {
  day: string;
  foodName: string;
  amount: string | null; // Combined amount string like "1.00 Bar"
  energy: number | null;
  protein: number | null;
  carbs: number | null;
  fat: number | null;
  fiber: number | null;
  sugars: number | null;
  sodium: number | null;
  group: string | null;
  category: string | null;
}

interface FoodLogTableProps {
  data: FoodServing[];
  selectedDate: string;
}

/**
 * Food log table showing meals for a specific day
 */
export const FoodLogTable: React.FC<FoodLogTableProps> = ({ data, selectedDate }) => {
  const [selectedItem, setSelectedItem] = useState<FoodServing | null>(null);

  // Filter servings for the selected date
  const filteredData = data.filter(item => item.day === selectedDate);

  const handleRowClick = (item: FoodServing) => {
    setSelectedItem(item);
  };

  const closeModal = () => {
    setSelectedItem(null);
  };

  const formatNumber = (val: number | string | null, decimals = 1) => {
    if (val === null || val === undefined) return '--';
    const num = typeof val === 'string' ? parseFloat(val) : val;
    if (isNaN(num)) return '--';
    return num.toFixed(decimals);
  };

  if (filteredData.length === 0) {
    return (
      <div className={styles.emptyState}>
        <h3>No food logged</h3>
        <p>Sync your Cronometer data to see your food log for this day</p>
      </div>
    );
  }

  return (
    <>
      <table className={styles.foodLogTable}>
        <thead>
          <tr>
            <th>Food</th>
            <th>Category</th>
            <th>Amount</th>
            <th>Calories</th>
            <th>Protein</th>
            <th>Carbs</th>
            <th>Fat</th>
          </tr>
        </thead>
        <tbody>
          {filteredData.map((item, index) => (
            <tr key={`${item.foodName}-${index}`} onClick={() => handleRowClick(item)}>
              <td className={styles.foodName}>{item.foodName}</td>
              <td className={styles.categoryCell}>
                {item.category && (
                  <span 
                    className={styles.categoryBadge} 
                    data-category={item.category.toLowerCase()}
                  >
                    {item.category}
                  </span>
                )}
              </td>
              <td className={styles.amount}>{item.amount || '--'}</td>
              <td className={styles.calories}>{formatNumber(item.energy, 0)}</td>
              <td className={styles.macro}>{formatNumber(item.protein)}</td>
              <td className={styles.macro}>{formatNumber(item.carbs)}</td>
              <td className={styles.macro}>{formatNumber(item.fat)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Food Detail Modal - V1 Style */}
      {selectedItem && (
        <div className={styles.modalOverlay} onClick={closeModal}>
          <div className={styles.foodModal} onClick={(e) => e.stopPropagation()}>
            <button className={styles.closeButtonFloating} onClick={closeModal}>×</button>
            <div className={styles.modalBody}>
              {/* Food Hero */}
              <div className={styles.foodHero}>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>🍽️</div>
                  <div className={styles.heroValue}>{selectedItem.foodName}</div>
                  <div className={styles.heroLabel}>FOOD ITEM</div>
                </div>
              </div>

              {/* Basic Info Section */}
              <div className={styles.section}>
                <div className={styles.foodInfoRow}>
                  <span className={styles.foodInfoLabel}>📁 Category</span>
                  <span className={styles.foodInfoValue}>{selectedItem.category || '--'}</span>
                </div>
                <div className={styles.foodInfoRow}>
                  <span className={styles.foodInfoLabel}>⚖️ Amount</span>
                  <span className={styles.foodInfoValue}>{selectedItem.amount || '--'}</span>
                </div>
                <div className={styles.foodInfoRow}>
                  <span className={styles.foodInfoLabel}>🍽️ Meal Group</span>
                  <span className={styles.foodInfoValue}>{selectedItem.group || 'Uncategorized'}</span>
                </div>
              </div>

              {/* Nutrition Summary Section */}
              <div className={styles.section}>
                <h3>🔥 Nutrition Facts</h3>
                <div className={styles.nutritionGrid}>
                  <div className={`${styles.nutritionCard} ${styles.caloriesCard}`}>
                    <div className={styles.nutritionIcon}>🔥</div>
                    <div className={styles.nutritionValue}>{formatNumber(selectedItem.energy, 0)} kcal</div>
                    <div className={styles.nutritionLabel}>CALORIES</div>
                  </div>
                  <div className={`${styles.nutritionCard} ${styles.proteinCard}`}>
                    <div className={styles.nutritionIcon}>🥩</div>
                    <div className={styles.nutritionValue}>{formatNumber(selectedItem.protein)}g</div>
                    <div className={styles.nutritionLabel}>PROTEIN</div>
                  </div>
                  <div className={`${styles.nutritionCard} ${styles.carbsCard}`}>
                    <div className={styles.nutritionIcon}>🍞</div>
                    <div className={styles.nutritionValue}>{formatNumber(selectedItem.carbs)}g</div>
                    <div className={styles.nutritionLabel}>CARBS</div>
                  </div>
                  <div className={`${styles.nutritionCard} ${styles.fatCard}`}>
                    <div className={styles.nutritionIcon}>🥑</div>
                    <div className={styles.nutritionValue}>{formatNumber(selectedItem.fat)}g</div>
                    <div className={styles.nutritionLabel}>FAT</div>
                  </div>
                </div>
              </div>

              {/* Additional Nutrients */}
              <div className={styles.section}>
                <h3>📊 Additional Nutrients</h3>
                <div className={styles.extraGrid}>
                  <div className={styles.extraItem}>
                    <span className={styles.extraLabel}>Fiber</span>
                    <span className={styles.extraValue}>{selectedItem.fiber !== null ? `${formatNumber(selectedItem.fiber)}g` : '--'}</span>
                  </div>
                  <div className={styles.extraItem}>
                    <span className={styles.extraLabel}>Sugar</span>
                    <span className={styles.extraValue}>{selectedItem.sugars !== null ? `${formatNumber(selectedItem.sugars)}g` : '--'}</span>
                  </div>
                  <div className={styles.extraItem}>
                    <span className={styles.extraLabel}>Sodium</span>
                    <span className={styles.extraValue}>{selectedItem.sodium !== null ? `${formatNumber(selectedItem.sodium, 0)}mg` : '--'}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default FoodLogTable;
