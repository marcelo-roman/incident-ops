import { themePreferences, type ThemePreference } from './themePreference';
import styles from './ThemeSwitcher.module.css';
import { useThemeRadioGroup } from './useThemeRadioGroup';

const optionLabels: Readonly<Record<ThemePreference, string>> = {
  system: 'Auto',
  light: 'Light',
  dark: 'Dark',
};

interface ThemeSwitcherProps {
  value: ThemePreference;
  onChange: (preference: ThemePreference) => void;
}

export function ThemeSwitcher({ value, onChange }: Readonly<ThemeSwitcherProps>) {
  const group = useThemeRadioGroup(value, onChange);
  return (
    <div className={styles.group} role="radiogroup" aria-label="Theme" onKeyDown={group.onKeyDown}>
      {themePreferences.map((preference) => (
        <button
          key={preference}
          type="button"
          role="radio"
          data-value={preference}
          className={styles.option}
          aria-checked={preference === value}
          tabIndex={group.tabIndexFor(preference)}
          onClick={() => {
            onChange(preference);
          }}
        >
          {optionLabels[preference]}
        </button>
      ))}
    </div>
  );
}
