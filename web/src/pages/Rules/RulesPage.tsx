import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { ApiError, api } from "../../api/client";
import type { PenaltyType, RuleRow } from "../../api/types";
import { RuleDiffView } from "../../components/RuleDiffView";
import { diffRuleRows } from "../../lib/ruleDiff";

const PENALTY_TYPES: PenaltyType[] = ["Amount", "Hour", "Daily"];

function emptyRule(): RuleRow {
  return { startHours: 0, endHours: 0, penaltyType: "Amount", value: 0, intervalHours: null };
}

export function RulesPage() {
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [draftRules, setDraftRules] = useState<RuleRow[] | null>(null);
  const [reason, setReason] = useState("");
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const listQuery = useQuery({ queryKey: ["rulesets"], queryFn: api.listRuleSets });
  const detailQuery = useQuery({
    queryKey: ["ruleset", selectedId],
    queryFn: () => api.getRuleSet(selectedId!),
    enabled: selectedId != null,
  });

  useEffect(() => {
    if (detailQuery.data) {
      setDraftRules(detailQuery.data.rules.map((r) => ({ ...r })));
    }
  }, [detailQuery.data]);

  useEffect(() => {
    if (!selectedId && listQuery.data && listQuery.data.length > 0) {
      setSelectedId(listQuery.data[0].id);
    }
  }, [listQuery.data, selectedId]);

  function invalidateAll() {
    queryClient.invalidateQueries({ queryKey: ["rulesets"] });
    queryClient.invalidateQueries({ queryKey: ["ruleset", selectedId] });
    queryClient.invalidateQueries({ queryKey: ["audit"] });
  }

  const saveMutation = useMutation({
    mutationFn: () => api.replaceRules(selectedId!, { rules: draftRules ?? [], reason: reason || undefined }),
    onSuccess: () => {
      setError(null);
      invalidateAll();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : String(e)),
  });

  const activateMutation = useMutation({
    mutationFn: () => api.activateRuleSet(selectedId!, { reason: reason || undefined }),
    onSuccess: () => {
      setError(null);
      invalidateAll();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : String(e)),
  });

  const createMutation = useMutation({
    mutationFn: () => api.createRuleSet({ name: newName, rules: draftRules ?? [emptyRule()], reason: reason || undefined }),
    onSuccess: (created) => {
      setError(null);
      setCreating(false);
      setSelectedId(created.id);
      invalidateAll();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : String(e)),
  });

  const selected = detailQuery.data;
  const isActive = selected?.isActive ?? false;
  // While creating a new version, the draft is editable even though the rule set it was
  // copied from (often the currently-selected active one) is itself read-only.
  const readOnly = isActive && !creating;
  const rows = draftRules ?? [];

  // Compared against the rule set the draft was loaded from (or copied from, when creating a
  // new version) - lets the UI show exactly what's pending and block a no-op save.
  const diffEntries = useMemo(() => diffRuleRows(selected?.rules ?? [], rows), [selected, rows]);
  const hasChanges = diffEntries.some((e) => e.type !== "unchanged");

  function updateRule(index: number, patch: Partial<RuleRow>) {
    setDraftRules((rs) => (rs ? rs.map((r, i) => (i === index ? { ...r, ...patch } : r)) : rs));
  }

  function removeRule(index: number) {
    setDraftRules((rs) => (rs ? rs.filter((_, i) => i !== index) : rs));
  }

  function addRule() {
    setDraftRules((rs) => [...(rs ?? []), emptyRule()]);
  }

  function startNewVersion() {
    setCreating(true);
    setNewName(selected ? `${selected.name} v2` : "New Rule Set");
    setDraftRules((selected?.rules ?? []).map((r) => ({ ...r, id: undefined })));
  }

  return (
    <div className="grid grid-cols-[220px_1fr] gap-6">
      <div className="space-y-2">
        <h2 className="text-lg font-semibold">Rule Sets</h2>
        <ul className="space-y-1">
          {listQuery.data?.map((rs) => (
            <li key={rs.id}>
              <button
                type="button"
                className={`w-full rounded px-2 py-1.5 text-left text-sm ${
                  selectedId === rs.id ? "bg-sky-600 text-white" : "hover:bg-slate-800"
                }`}
                onClick={() => {
                  setCreating(false);
                  setSelectedId(rs.id);
                }}
              >
                {rs.name}
                {rs.isActive && <span className="ml-1 text-xs text-emerald-300">● active</span>}
              </button>
            </li>
          ))}
        </ul>
        <button
          type="button"
          className="mt-2 w-full rounded border border-slate-600 px-2 py-1.5 text-sm hover:bg-slate-800"
          onClick={startNewVersion}
        >
          + New version
        </button>
      </div>

      <div className="space-y-4">
        {creating ? (
          <input
            className="w-full rounded border border-slate-700 bg-slate-800 px-2 py-1.5 text-lg font-semibold"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
          />
        ) : (
          selected && (
            <div className="flex items-center gap-3">
              <h2 className="text-lg font-semibold">{selected.name}</h2>
              {isActive && <span className="rounded bg-emerald-900 px-2 py-0.5 text-xs text-emerald-300">Active</span>}
              {isActive && (
                <span className="text-xs text-slate-500">Active rule sets are read-only - create a new version to edit.</span>
              )}
            </div>
          )
        )}

        <div className="overflow-x-auto rounded border border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-900 text-left text-slate-400">
              <tr>
                <th className="px-3 py-2">Start (h)</th>
                <th className="px-3 py-2">End (h)</th>
                <th className="px-3 py-2">Type</th>
                <th className="px-3 py-2">Value</th>
                <th className="px-3 py-2">Interval (h)</th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody>
              {rows.map((rule, i) => (
                <tr key={i} className="border-t border-slate-800">
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      disabled={readOnly}
                      className="w-20 rounded border border-slate-700 bg-slate-800 px-2 py-1 disabled:opacity-50"
                      value={rule.startHours}
                      onChange={(e) => updateRule(i, { startHours: Number(e.target.value) })}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      disabled={readOnly}
                      className="w-20 rounded border border-slate-700 bg-slate-800 px-2 py-1 disabled:opacity-50"
                      value={rule.endHours}
                      onChange={(e) => updateRule(i, { endHours: Number(e.target.value) })}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <select
                      disabled={readOnly}
                      className="rounded border border-slate-700 bg-slate-800 px-2 py-1 disabled:opacity-50"
                      value={rule.penaltyType}
                      onChange={(e) => updateRule(i, { penaltyType: e.target.value as PenaltyType })}
                    >
                      {PENALTY_TYPES.map((t) => (
                        <option key={t} value={t}>
                          {t}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      step="0.01"
                      disabled={readOnly}
                      className="w-20 rounded border border-slate-700 bg-slate-800 px-2 py-1 disabled:opacity-50"
                      value={rule.value}
                      onChange={(e) => updateRule(i, { value: Number(e.target.value) })}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      step="0.01"
                      disabled={readOnly}
                      placeholder="none"
                      className="w-20 rounded border border-slate-700 bg-slate-800 px-2 py-1 disabled:opacity-50"
                      value={rule.intervalHours ?? ""}
                      onChange={(e) =>
                        updateRule(i, { intervalHours: e.target.value === "" ? null : Number(e.target.value) })
                      }
                    />
                  </td>
                  <td className="px-3 py-2">
                    {!readOnly && (
                      <button type="button" className="text-xs text-red-400 hover:underline" onClick={() => removeRule(i)}>
                        remove
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {!readOnly && (
          <button type="button" className="rounded bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700" onClick={addRule}>
            + Add row
          </button>
        )}

        {!readOnly && (
          <div className="rounded border border-slate-800 bg-slate-900 p-3">
            <h3 className="mb-1 text-xs font-semibold tracking-wide text-slate-500 uppercase">
              Pending changes {creating && "(vs. the version this was copied from)"}
            </h3>
            <RuleDiffView entries={diffEntries} />
          </div>
        )}

        {!readOnly && (
          <div className="space-y-2 rounded border border-slate-800 bg-slate-900 p-3">
            <label className="block text-sm text-slate-300">
              Reason (recorded in the audit trail)
              <input
                className="mt-1 w-full rounded border border-slate-700 bg-slate-800 px-2 py-1.5"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="e.g. Updated hourly threshold per new union agreement"
              />
            </label>
            <div className="flex gap-2">
              {creating ? (
                <button
                  type="button"
                  disabled={createMutation.isPending || !hasChanges || !newName.trim()}
                  title={!hasChanges ? "No changes from the version this was copied from" : undefined}
                  className="rounded bg-sky-600 px-4 py-1.5 text-sm font-medium hover:bg-sky-500 disabled:opacity-50"
                  onClick={() => createMutation.mutate()}
                >
                  {createMutation.isPending ? "Creating..." : "Create version"}
                </button>
              ) : (
                <>
                  <button
                    type="button"
                    disabled={saveMutation.isPending || !hasChanges}
                    title={!hasChanges ? "No changes to save" : undefined}
                    className="rounded bg-sky-600 px-4 py-1.5 text-sm font-medium hover:bg-sky-500 disabled:opacity-50"
                    onClick={() => saveMutation.mutate()}
                  >
                    {saveMutation.isPending ? "Saving..." : "Save changes"}
                  </button>
                  <button
                    type="button"
                    disabled={activateMutation.isPending}
                    className="rounded border border-emerald-600 px-4 py-1.5 text-sm font-medium text-emerald-300 hover:bg-emerald-900/40 disabled:opacity-50"
                    onClick={() => activateMutation.mutate()}
                  >
                    {activateMutation.isPending ? "Activating..." : "Activate this version"}
                  </button>
                </>
              )}
            </div>
          </div>
        )}

        {error && <p className="text-sm text-red-400">{error}</p>}
      </div>
    </div>
  );
}
