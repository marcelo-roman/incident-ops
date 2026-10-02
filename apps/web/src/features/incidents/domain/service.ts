export const serviceTiers = ['Tier1', 'Tier2', 'Tier3'] as const;
export type ServiceTier = (typeof serviceTiers)[number];

export interface Service {
  id: string;
  name: string;
  tier: ServiceTier;
  ownerTeam: string;
}
