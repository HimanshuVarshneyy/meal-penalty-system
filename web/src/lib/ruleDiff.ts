import type { RuleRow } from "../api/types";

export interface RuleFieldChange {
  label: string;
  before: string;
  after: string;
}

export type RuleRowDiffEntry =
  | { type: "added"; index: number; row: RuleRow }
  | { type: "removed"; index: number; row: RuleRow }
  | { type: "changed"; index: number; before: RuleRow; after: RuleRow; changes: RuleFieldChange[] }
  | { type: "unchanged"; index: number; row: RuleRow };

function formatInterval(value: number | null | undefined): string {
  return value === null || value === undefined ? "none" : `${value}h`;
}

function describeRow(row: RuleRow): string {
  return `${row.startHours}h - ${row.endHours}h, ${row.penaltyType}, value ${row.value}, interval ${formatInterval(row.intervalHours)}`;
}

function fieldChanges(before: RuleRow, after: RuleRow): RuleFieldChange[] {
  const changes: RuleFieldChange[] = [];
  if (before.startHours !== after.startHours) {
    changes.push({ label: "Start", before: `${before.startHours}h`, after: `${after.startHours}h` });
  }
  if (before.endHours !== after.endHours) {
    changes.push({ label: "End", before: `${before.endHours}h`, after: `${after.endHours}h` });
  }
  if (before.penaltyType !== after.penaltyType) {
    changes.push({ label: "Type", before: before.penaltyType, after: after.penaltyType });
  }
  if (before.value !== after.value) {
    changes.push({ label: "Value", before: String(before.value), after: String(after.value) });
  }
  if (before.intervalHours !== after.intervalHours) {
    changes.push({ label: "Interval", before: formatInterval(before.intervalHours), after: formatInterval(after.intervalHours) });
  }
  return changes;
}

function rowsEqual(a: RuleRow, b: RuleRow): boolean {
  return (
    a.startHours === b.startHours &&
    a.endHours === b.endHours &&
    a.penaltyType === b.penaltyType &&
    a.value === b.value &&
    a.intervalHours === b.intervalHours
  );
}

/**
 * Rows are matched positionally (row N in "before" vs row N in "after"). Good enough for the
 * common edits - tweak a row, add one at the end, remove the last one - without needing a full
 * list-alignment algorithm.
 */
export function diffRuleRows(before: RuleRow[], after: RuleRow[]): RuleRowDiffEntry[] {
  const length = Math.max(before.length, after.length);
  const entries: RuleRowDiffEntry[] = [];

  for (let i = 0; i < length; i++) {
    const b = before[i];
    const a = after[i];

    if (b && a) {
      entries.push(
        rowsEqual(b, a)
          ? { type: "unchanged", index: i, row: a }
          : { type: "changed", index: i, before: b, after: a, changes: fieldChanges(b, a) },
      );
    } else if (!b && a) {
      entries.push({ type: "added", index: i, row: a });
    } else if (b && !a) {
      entries.push({ type: "removed", index: i, row: b });
    }
  }

  return entries;
}

export function hasRuleChanges(before: RuleRow[], after: RuleRow[]): boolean {
  return diffRuleRows(before, after).some((e) => e.type !== "unchanged");
}

export { describeRow };
