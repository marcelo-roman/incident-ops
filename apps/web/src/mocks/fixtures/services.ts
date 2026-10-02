import type { Service } from '../../features/incidents';

export const serviceFixtures: Service[] = [
  { id: 'checkout', name: 'Checkout', tier: 'Tier1', ownerTeam: 'Commerce' },
  { id: 'payments-gateway', name: 'Payments gateway', tier: 'Tier1', ownerTeam: 'Payments' },
  { id: 'identity', name: 'Identity', tier: 'Tier1', ownerTeam: 'Identity & Access' },
  { id: 'notifications', name: 'Notifications', tier: 'Tier2', ownerTeam: 'Engagement' },
  { id: 'search', name: 'Search', tier: 'Tier2', ownerTeam: 'Discovery' },
  { id: 'reporting', name: 'Reporting', tier: 'Tier3', ownerTeam: 'Data Platform' },
  { id: 'platform', name: 'Platform', tier: 'Tier1', ownerTeam: 'Site Reliability' },
];
