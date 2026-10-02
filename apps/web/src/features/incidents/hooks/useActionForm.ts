import { zodResolver } from '@hookform/resolvers/zod';
import type { UseMutationResult } from '@tanstack/react-query';
import type { SubmitEvent } from 'react';
import { useForm, type DefaultValues, type FieldValues } from 'react-hook-form';
import type { z } from 'zod';

interface ActionFormOptions<TValues extends FieldValues> {
  schema: z.ZodType<TValues, TValues>;
  defaultValues: DefaultValues<TValues>;
  mutation: UseMutationResult<unknown, Error, TValues>;
  successMessage: string;
  onDone: (message: string) => void;
}

export function useActionForm<TValues extends FieldValues>(options: ActionFormOptions<TValues>) {
  const { schema, defaultValues, mutation, successMessage, onDone } = options;
  const form = useForm<TValues>({ resolver: zodResolver(schema), defaultValues });
  const submit = form.handleSubmit((values) => {
    mutation.mutate(values, {
      onSuccess: () => {
        onDone(successMessage);
      },
    });
  });
  return {
    register: form.register,
    errors: form.formState.errors,
    isPending: mutation.isPending,
    error: mutation.error,
    onSubmit: (event: SubmitEvent<HTMLFormElement>) => void submit(event),
  };
}
