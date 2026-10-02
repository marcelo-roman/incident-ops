import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { useDeclareIncident } from '../api/useDeclareIncident';
import { declareIncidentSchema, type DeclareIncidentValues } from '../domain/declareSchema';

export function useDeclareIncidentForm() {
  const navigate = useNavigate();
  const mutation = useDeclareIncident();
  const form = useForm<DeclareIncidentValues>({
    resolver: zodResolver(declareIncidentSchema),
    defaultValues: { title: '', serviceId: '', description: '' },
  });
  const submit = form.handleSubmit((values) => {
    mutation.mutate(values, {
      onSuccess: (incident) => {
        void navigate(`/incidents/${incident.id}`);
      },
    });
  });
  return { form, submit, mutation };
}
