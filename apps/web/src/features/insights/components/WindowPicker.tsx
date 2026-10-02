import { Button } from '../../../shared/ui/Button';
import { insightWindows, type InsightWindow } from '../domain/insights';
import styles from './Insights.module.css';

interface WindowPickerProps {
  value: InsightWindow;
  onChange: (days: InsightWindow) => void;
}

export function WindowPicker({ value, onChange }: Readonly<WindowPickerProps>) {
  return (
    <div className={styles.windows} role="group" aria-label="Analysis window">
      {insightWindows.map((days) => (
        <Button
          key={days}
          aria-pressed={days === value}
          onClick={() => {
            onChange(days);
          }}
        >
          {days} days
        </Button>
      ))}
    </div>
  );
}
