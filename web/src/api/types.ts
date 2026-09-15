export type PenaltyType = "Hour" | "Amount" | "Daily";

export interface MealBreakInput {
  mealStart: number;
  mealEnd: number;
}

export interface TimesheetEntryInput {
  label: string;
  workDate: string;
  inHours: number;
  outHours: number;
  hourlyRate: number;
  mealBreaks: MealBreakInput[];
}

export interface WindowResult {
  index: number;
  start: number;
  end: number;
  length: number;
}

export interface PenaltyTriggerResult {
  windowIndex: number;
  windowStart: number;
  windowEnd: number;
  ruleId: number | null;
  triggerCount: number;
  penaltyAmount: number;
  description: string;
}

export interface DayResult {
  label: string;
  workDate: string;
  hourlyRate: number;
  paidHours: number;
  totalPenalty: number;
  totalPaidAmount: number;
  windows: WindowResult[];
  triggers: PenaltyTriggerResult[];
}

export interface RuleRow {
  id?: number;
  startHours: number;
  endHours: number;
  penaltyType: PenaltyType;
  value: number;
  intervalHours: number | null;
}

export interface RuleSetSummary {
  id: number;
  name: string;
  isActive: boolean;
  effectiveFrom: string;
  createdAt: string;
  createdBy: string;
}

export interface RuleSetDetail extends RuleSetSummary {
  rules: RuleRow[];
}

export interface CalculationRunDetail {
  id: number;
  calculationRunId: number;
  windowIndex: number;
  windowStart: number;
  windowEnd: number;
  ruleId: number | null;
  triggerCount: number;
  penaltyAmount: number;
  description: string;
}

export interface CalculationRun {
  id: number;
  timesheetEntryId: number;
  ruleSetId: number;
  label: string;
  workDate: string;
  hourlyRate: number;
  paidHours: number;
  totalPenalty: number;
  totalPaidAmount: number;
  createdAt: string;
  createdBy: string;
  details: CalculationRunDetail[];
}

export type AuditAction = "Create" | "Update" | "Delete" | "Activate";

export interface RuleAuditEntry {
  id: number;
  ruleSetId: number;
  ruleId: number | null;
  action: AuditAction;
  changedBy: string;
  changedAt: string;
  oldValueJson: string | null;
  newValueJson: string | null;
  reason: string | null;
}

export interface SimulateSavedResult {
  runId: number;
  result: DayResult;
}
