/**
 * Which section of the equipment screen an item belongs in.
 *
 * Presentation, so it lives here: the API answers a flat vocabulary because *what* a gym can hold
 * is its business, and *how the question is asked* is this tier's. Around thirty checkboxes read
 * as a wall ungrouped, which is the shape the engineer already called limited in `M2` — grouping
 * is the fix, not a shorter vocabulary (`ADR-022`).
 *
 * An item missing from here falls into `other` rather than disappearing. A checkbox that vanished
 * because nobody mapped it would take a whole gym out of a user's answer silently.
 */
export const EQUIPMENT_GROUPS = [
  "freeWeights",
  "benchesAndRacks",
  "cables",
  "machines",
  "other",
] as const;

export type EquipmentGroup = (typeof EQUIPMENT_GROUPS)[number];

const BY_ITEM: Record<string, EquipmentGroup> = {
  Bodyweight: "freeWeights",
  Barbell: "freeWeights",
  WeightPlates: "freeWeights",
  Dumbbells: "freeWeights",

  Bench: "benchesAndRacks",
  AdjustableBench: "benchesAndRacks",
  SquatRack: "benchesAndRacks",
  PullUpBar: "benchesAndRacks",
  BackExtensionBench: "benchesAndRacks",
  SmithMachine: "benchesAndRacks",

  CableStation: "cables",
  LatPulldownStation: "cables",

  LegPressMachine: "machines",
  HackSquatMachine: "machines",
  LegExtensionMachine: "machines",
  SeatedLegCurlMachine: "machines",
  LyingLegCurlMachine: "machines",
  StandingLegCurlMachine: "machines",
  HipAbductionMachine: "machines",
  SeatedCalfRaiseMachine: "machines",
  StandingCalfRaiseMachine: "machines",
  ChestPressMachine: "machines",
  PecDeckMachine: "machines",
  SeatedRowMachine: "machines",
  HighRowMachine: "machines",
  ShoulderPressMachine: "machines",
  PreacherCurlMachine: "machines",
  AbdominalMachine: "machines",
};

export function groupOf(item: string): EquipmentGroup {
  return BY_ITEM[item] ?? "other";
}
