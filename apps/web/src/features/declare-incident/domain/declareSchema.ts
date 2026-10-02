import { z } from 'zod';
import { severities } from '../../incidents';

export const declareIncidentSchema = z.object({
  title: z
    .string()
    .trim()
    .min(5, 'Give the incident a title of at least 5 characters.')
    .max(140, 'Keep the title under 140 characters.'),
  serviceId: z.string().min(1, 'Choose the affected service.'),
  severity: z.enum(severities, { error: 'Choose a severity.' }),
  description: z
    .string()
    .trim()
    .min(10, 'Describe the impact in at least 10 characters.')
    .max(4000, 'Keep the description under 4000 characters.'),
});

export type DeclareIncidentValues = z.infer<typeof declareIncidentSchema>;
