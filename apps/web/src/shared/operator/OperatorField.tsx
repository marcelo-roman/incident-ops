import { useOperator } from './useOperator';
import styles from './OperatorField.module.css';

export function OperatorField() {
  const { operator, setOperator } = useOperator();
  return (
    <label className={styles.field}>
      On shift as
      <input
        className={styles.input}
        value={operator}
        placeholder="Your name"
        autoComplete="name"
        onChange={(event) => {
          setOperator(event.target.value);
        }}
      />
    </label>
  );
}
