import { describeError } from '../../../shared/http/problem';
import { Button } from '../../../shared/ui/Button';
import { ButtonLink } from '../../../shared/ui/ButtonLink';
import { SelectField, TextAreaField, TextField } from '../../../shared/ui/Field';
import { PageHeader } from '../../../shared/ui/PageHeader';
import { Panel } from '../../../shared/ui/Panel';
import { useServices } from '../../incidents';
import styles from './DeclareIncident.module.css';
import { SeverityPicker } from '../components/SeverityPicker';
import { useDeclareIncidentForm } from '../hooks/useDeclareIncidentForm';

export function DeclareIncidentPage() {
  const { form, submit, mutation } = useDeclareIncidentForm();
  const { data: services = [] } = useServices();
  const { errors } = form.formState;
  return (
    <>
      <PageHeader
        title="Declare incident"
        summary="The SLA clock starts when you declare. Sev1 and Sev2 page the primary on call immediately."
      />
      <Panel title="Incident">
        <form className={styles.form} aria-label="Declare incident" noValidate onSubmit={(event) => void submit(event)}>
          <div className={styles.row}>
            <TextField
              label="Title"
              placeholder="Checkout returns 502 for card payments"
              error={errors.title?.message}
              {...form.register('title')}
            />
            <SelectField label="Affected service" error={errors.serviceId?.message} {...form.register('serviceId')}>
              <option value="">Choose a service</option>
              {services.map((service) => (
                <option key={service.id} value={service.id}>
                  {service.name}
                </option>
              ))}
            </SelectField>
          </div>
          <SeverityPicker registration={form.register('severity')} error={errors.severity?.message} />
          <TextAreaField
            label="Impact"
            hint="Who is affected, since when, and what they see."
            error={errors.description?.message}
            {...form.register('description')}
          />
          {mutation.error !== null && (
            <p className={styles.error} role="alert">
              {describeError(mutation.error)}
            </p>
          )}
          <div className={styles.footer}>
            <Button type="submit" variant="primary" disabled={mutation.isPending}>
              {mutation.isPending && 'Declaring'}
              {!mutation.isPending && 'Declare incident'}
            </Button>
            <ButtonLink to="/incidents">Cancel</ButtonLink>
          </div>
        </form>
      </Panel>
    </>
  );
}
