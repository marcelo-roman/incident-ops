import { z } from 'zod';

const actor = z
  .string()
  .trim()
  .min(2, 'Enter the name of the person taking this action.')
  .max(100, 'Keep the name under 100 characters.');

export const acknowledgeSchema = z.object({ actor });

export const mitigateSchema = z.object({
  actor,
  note: z
    .string()
    .trim()
    .min(5, 'Describe what was done to mitigate, in at least 5 characters.')
    .max(2000, 'Keep the note under 2000 characters.'),
});

export const resolveSchema = z.object({
  actor,
  rootCause: z
    .string()
    .trim()
    .min(10, 'Describe the root cause in at least 10 characters.')
    .max(4000, 'Keep the root cause under 4000 characters.'),
});

export const noteSchema = z.object({
  actor,
  message: z
    .string()
    .trim()
    .min(1, 'Write the note before adding it.')
    .max(4000, 'Keep the note under 4000 characters.'),
});

export type AcknowledgeValues = z.infer<typeof acknowledgeSchema>;
export type MitigateValues = z.infer<typeof mitigateSchema>;
export type ResolveValues = z.infer<typeof resolveSchema>;
export type NoteValues = z.infer<typeof noteSchema>;
