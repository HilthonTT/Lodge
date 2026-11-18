import { addDays } from "date-fns";

import type { CancellationPolicy } from "@/types/api";

/**
 * Refund tiers per policy, mirroring the API: cancelling at least `daysBefore`
 * days before check-in refunds `refundPercent`; the last tier has no limit.
 */
export const POLICY_TIERS: Record<CancellationPolicy, { daysBefore: number | null; refundPercent: number }[]> = {
  Flexible: [
    { daysBefore: 1, refundPercent: 100 },
    { daysBefore: null, refundPercent: 0 },
  ],
  Moderate: [
    { daysBefore: 5, refundPercent: 100 },
    { daysBefore: null, refundPercent: 50 },
  ],
  Strict: [
    { daysBefore: 14, refundPercent: 100 },
    { daysBefore: 7, refundPercent: 50 },
    { daysBefore: null, refundPercent: 0 },
  ],
};

export type TimelineTier = {
  /** Last day of this tier; null for the final tier (until check-in). */
  until: Date | null;
  refundPercent: number;
  /** Days before check-in where this tier ends (null for the final tier). */
  daysBefore: number | null;
};

/** Tiers of a policy, with concrete dates when the check-in date is known. */
export function policyTimeline(policy: CancellationPolicy, checkIn?: Date): TimelineTier[] {
  return (POLICY_TIERS[policy] ?? POLICY_TIERS.Moderate).map((tier) => ({
    ...tier,
    until: checkIn && tier.daysBefore !== null ? addDays(checkIn, -tier.daysBefore) : null,
  }));
}
