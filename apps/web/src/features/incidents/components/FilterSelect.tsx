import { SelectField } from '../../../shared/ui/Field';

export interface FilterOption {
  value: string;
  label: string;
}

interface FilterSelectProps {
  label: string;
  anyLabel: string;
  value: string | undefined;
  options: readonly FilterOption[];
  onChange: (value: string) => void;
}

export function FilterSelect({ label, anyLabel, value, options, onChange }: Readonly<FilterSelectProps>) {
  return (
    <SelectField
      label={label}
      value={value ?? ''}
      onChange={(event) => {
        onChange(event.target.value);
      }}
    >
      <option value="">{anyLabel}</option>
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </SelectField>
  );
}
