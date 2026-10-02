import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { useSignIn } from '../api/useSignIn';
import { loginSchema, type LoginValues } from '../domain/loginSchema';

export function useLoginForm(returnTo: string) {
  const navigate = useNavigate();
  const mutation = useSignIn();
  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: '', password: '' },
  });
  const submit = form.handleSubmit((values) => {
    mutation.mutate(values, {
      onSuccess: () => {
        void navigate(returnTo, { replace: true });
      },
      onError: () => {
        form.resetField('password');
      },
    });
  });
  return { form, submit, mutation };
}
