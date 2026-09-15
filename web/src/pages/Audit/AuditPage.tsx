import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../../api/client";
import type { RuleRow } from "../../api/types";
import { RuleDiffView } from "../../components/RuleDiffView";
import { diffRuleRows } from "../../lib/ruleDiff";

const ACTION_COLORS: Record<string, string> = {
  Create: "bg-emerald-900 text-emerald-300",
  Update: "bg-amber-900 text-amber-300",
  Activate: "bg-sky-900 text-sky-300",
  Delete: "bg-red-900 text-red-300",
};

function parseRules(json: string | null): RuleRow[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    return Array.isArray(parsed) ? (parsed as RuleRow[]) : [];
  } catch {
    return [];
  }
}

export function AuditPage() {
  const [ruleSetId, setRuleSetId] = useState<number | "">("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");

  const ruleSetsQuery = useQuery({ queryKey: ["rulesets"], queryFn: api.listRuleSets });
  const auditQuery = useQuery({
    queryKey: ["audit", ruleSetId, from, to],
    queryFn: () =>
      api.listRuleAudit({
        ruleSetId: ruleSetId === "" ? undefined : ruleSetId,
        from: from || undefined,
        to: to || undefined,
      }),
  });

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold">Rule Change Audit Trail</h2>

      <div className="flex flex-wrap items-end gap-3 rounded border border-slate-800 bg-slate-900 p-3">
        <label className="text-sm text-slate-300">
          Rule set
          <select
            className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1"
            value={ruleSetId}
            onChange={(e) => setRuleSetId(e.target.value === "" ? "" : Number(e.target.value))}
          >
            <option value="">All</option>
            {ruleSetsQuery.data?.map((rs) => (
              <option key={rs.id} value={rs.id}>
                {rs.name}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm text-slate-300">
          From
          <input type="date" className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="text-sm text-slate-300">
          To
          <input type="date" className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
      </div>

      <div className="overflow-x-auto rounded border border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-900 text-left text-slate-400">
            <tr>
              <th className="px-3 py-2">When</th>
              <th className="px-3 py-2">Action</th>
              <th className="px-3 py-2">Rule set</th>
              <th className="px-3 py-2">Changed by</th>
              <th className="px-3 py-2">Reason</th>
              <th className="px-3 py-2">Diff</th>
            </tr>
          </thead>
          <tbody>
            {auditQuery.data?.map((entry) => (
              <tr key={entry.id} className="border-t border-slate-800 align-top">
                <td className="px-3 py-2 whitespace-nowrap text-slate-400">{new Date(entry.changedAt).toLocaleString()}</td>
                <td className="px-3 py-2">
                  <span className={`rounded px-2 py-0.5 text-xs ${ACTION_COLORS[entry.action] ?? "bg-slate-800"}`}>{entry.action}</span>
                </td>
                <td className="px-3 py-2">#{entry.ruleSetId}</td>
                <td className="px-3 py-2">{entry.changedBy}</td>
                <td className="px-3 py-2 text-slate-400">{entry.reason ?? "—"}</td>
                <td className="px-3 py-2 max-w-md">
                  {entry.oldValueJson || entry.newValueJson ? (
                    <RuleDiffView entries={diffRuleRows(parseRules(entry.oldValueJson), parseRules(entry.newValueJson))} />
                  ) : (
                    <span className="text-xs text-slate-500">—</span>
                  )}
                </td>
              </tr>
            ))}
            {auditQuery.data?.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  No rule changes recorded yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
