import { Button } from '../../../shared/ui/Button';
import { TextField } from '../../../shared/ui/Field';
import { describeLoginError } from '../domain/loginError';
import { useLoginForm } from '../hooks/useLoginForm';
import styles from './LoginForm.module.css';

export function LoginForm({ returnTo }: Readonly<{ returnTo: string }>) {
  const { form, submit, mutation } = useLoginForm(returnTo);
  const { errors } = form.formState;
  return (
    <form className={styles.form} aria-label="Sign in" noValidate onSubmit={(event) => void submit(event)}>
      <TextField
        label="Username"
        autoComplete="username"
        autoCapitalize="none"
        spellCheck={false}
        error={errors.username?.message}
        {...form.register('username')}
      />
      <TextField
        label="Password"
        type="password"
        autoComplete="current-password"
        error={errors.password?.message}
        {...form.register('password')}
      />
      <div aria-live="assertive">
        {mutation.error !== null && (
          <p className={styles.error} role="alert">
            {describeLoginError(mutation.error)}
          </p>
        )}
      </div>
      <Button type="submit" variant="primary" disabled={mutation.isPending}>
        {mutation.isPending && 'Signing in'}
        {!mutation.isPending && 'Sign in'}
      </Button>
    </form>
  );
}
