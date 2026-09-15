import { describeRow, type RuleRowDiffEntry } from "../lib/ruleDiff";

export function RuleDiffView({ entries }: { entries: RuleRowDiffEntry[] }) {
  const changed = entries.filter((e) => e.type !== "unchanged");

  if (changed.length === 0) {
    return <p className="text-xs text-slate-500">No changes.</p>;
  }

  return (
    <ul className="space-y-1 text-xs">
      {changed.map((entry) => {
        if (entry.type === "added") {
          return (
            <li key={entry.index} className="text-emerald-400">
              + Row {entry.index + 1} added: {describeRow(entry.row)}
            </li>
          );
        }
        if (entry.type === "removed") {
          return (
            <li key={entry.index} className="text-red-400">
              − Row {entry.index + 1} removed: {describeRow(entry.row)}
            </li>
          );
        }
        return (
          <li key={entry.index} className="text-amber-400">
            ~ Row {entry.index + 1} changed:{" "}
            {entry.changes.map((c, i) => (
              <span key={c.label} className="text-slate-300">
                {i > 0 && ", "}
                {c.label} {c.before} → {c.after}
              </span>
            ))}
          </li>
        );
      })}
    </ul>
  );
}
