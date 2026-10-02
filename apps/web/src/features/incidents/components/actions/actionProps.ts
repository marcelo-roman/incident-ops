export interface IncidentActionFormProps {
  incidentId: string;
  onDone: (message: string) => void;
  onCancel: () => void;
}
