import { useMutation, useQuery } from "@tanstack/react-query";
import { Fragment, useState } from "react";
import { api } from "../../api/client";
import type { DayResult, TimesheetEntryInput } from "../../api/types";
import { newRow, sampleRows, type SimRow } from "./types";

function dayLabelFromDate(dateStr: string): string {
  const d = new Date(`${dateStr}T00:00:00`);
  if (dateStr === "" || Number.isNaN(d.getTime())) return "";
  return d.toLocaleDateString("en-US", { weekday: "short" });
}

function toEntry(row: SimRow): TimesheetEntryInput {
  return {
    label: row.label || "Untitled",
    workDate: row.workDate || new Date().toISOString().slice(0, 10),
    inHours: parseFloat(row.inHours) || 0,
    outHours: parseFloat(row.outHours) || 0,
    hourlyRate: parseFloat(row.hourlyRate) || 0,
    mealBreaks: row.meals
      .filter((m) => m.mealStart.trim() !== "" && m.mealEnd.trim() !== "")
      .map((m) => ({ mealStart: parseFloat(m.mealStart), mealEnd: parseFloat(m.mealEnd) })),
  };
}

export function SimulatePage() {
  const [rows, setRows] = useState<SimRow[]>(sampleRows);
  const [ruleSetId, setRuleSetId] = useState<number | null>(null);
  const [results, setResults] = useState<DayResult[] | null>(null);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  const ruleSetsQuery = useQuery({ queryKey: ["rulesets"], queryFn: api.listRuleSets });
  const activeRuleSetId = ruleSetId ?? ruleSetsQuery.data?.find((r) => r.isActive)?.id ?? ruleSetsQuery.data?.[0]?.id;

  const simulateMutation = useMutation({
    mutationFn: () => api.simulate({ ruleSetId: activeRuleSetId!, entries: rows.map(toEntry) }),
    onSuccess: (data) => {
      setResults(data);
      setSaveMessage(null);
    },
  });

  const saveMutation = useMutation({
    mutationFn: () => api.simulateAndSave({ ruleSetId: activeRuleSetId!, entries: rows.map(toEntry) }),
    onSuccess: (data) => {
      setResults(data.map((d) => d.result));
      setSaveMessage(`Saved ${data.length} day(s) to Reports.`);
    },
  });

  function updateRow(key: string, patch: Partial<SimRow>) {
    setRows((rs) => rs.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function updateMeal(key: string, mealIndex: number, patch: Partial<{ mealStart: string; mealEnd: string }>) {
    setRows((rs) =>
      rs.map((r) =>
        r.key === key
          ? { ...r, meals: r.meals.map((m, i) => (i === mealIndex ? { ...m, ...patch } : m)) }
          : r,
      ),
    );
  }

  function addMeal(key: string) {
    setRows((rs) =>
      rs.map((r) => (r.key === key ? { ...r, meals: [...r.meals, { mealStart: "", mealEnd: "" }] } : r)),
    );
  }

  function removeMeal(key: string, mealIndex: number) {
    setRows((rs) =>
      rs.map((r) => (r.key === key ? { ...r, meals: r.meals.filter((_, i) => i !== mealIndex) } : r)),
    );
  }

  function removeRow(key: string) {
    setRows((rs) => rs.filter((r) => r.key !== key));
  }

  function toggleExpanded(label: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(label)) next.delete(label);
      else next.add(label);
      return next;
    });
  }

  const canRun = activeRuleSetId != null && rows.length > 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Simulation</h2>
        <label className="flex items-center gap-2 text-sm">
          Rule set
          <select
            className="rounded border border-slate-600 bg-slate-800 px-2 py-1"
            value={activeRuleSetId ?? ""}
            onChange={(e) => setRuleSetId(Number(e.target.value))}
          >
            {ruleSetsQuery.data?.map((rs) => (
              <option key={rs.id} value={rs.id}>
                {rs.name} {rs.isActive ? "(active)" : ""}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="overflow-x-auto rounded border border-slate-800">
        <table className="w-full min-w-[820px] text-sm">
          <thead className="bg-slate-900 text-left text-slate-400">
            <tr>
              <th className="px-3 py-2">Day</th>
              <th className="px-3 py-2">Work date</th>
              <th className="px-3 py-2">In</th>
              <th className="px-3 py-2">Meal(s)</th>
              <th className="px-3 py-2">Out</th>
              <th className="px-3 py-2">Rate</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.key} className="border-t border-slate-800">
                <td className="px-3 py-2">
                  <input
                    readOnly
                    className="w-20 cursor-not-allowed rounded border border-slate-700 bg-slate-900 px-2 py-1 text-slate-400"
                    value={row.label}
                    placeholder="—"
                  />
                </td>
                <td className="px-3 py-2">
                  <input
                    type="date"
                    className="rounded border border-slate-700 bg-slate-800 px-2 py-1"
                    value={row.workDate}
                    onChange={(e) => {
                      const workDate = e.target.value;
                      updateRow(row.key, { workDate, label: dayLabelFromDate(workDate) });
                    }}
                  />
                </td>
                <td className="px-3 py-2">
                  <input
                    className="w-16 rounded border border-slate-700 bg-slate-800 px-2 py-1"
                    value={row.inHours}
                    placeholder="8.0"
                    onChange={(e) => updateRow(row.key, { inHours: e.target.value })}
                  />
                </td>
                <td className="px-3 py-2">
                  <div className="flex flex-col gap-1">
                    {row.meals.map((m, i) => (
                      <div key={i} className="flex items-center gap-1">
                        <input
                          className="w-16 rounded border border-slate-700 bg-slate-800 px-2 py-1"
                          value={m.mealStart}
                          placeholder="start"
                          onChange={(e) => updateMeal(row.key, i, { mealStart: e.target.value })}
                        />
                        <input
                          className="w-16 rounded border border-slate-700 bg-slate-800 px-2 py-1"
                          value={m.mealEnd}
                          placeholder="end"
                          onChange={(e) => updateMeal(row.key, i, { mealEnd: e.target.value })}
                        />
                        {i > 0 && (
                          <button
                            type="button"
                            title="Remove this meal break"
                            className="text-xs text-red-400 hover:underline"
                            onClick={() => removeMeal(row.key, i)}
                          >
                            ✕
                          </button>
                        )}
                      </div>
                    ))}
                    <button
                      type="button"
                      className="w-fit text-xs text-sky-400 hover:underline"
                      onClick={() => addMeal(row.key)}
                    >
                      + add meal
                    </button>
                  </div>
                </td>
                <td className="px-3 py-2">
                  <input
                    className="w-16 rounded border border-slate-700 bg-slate-800 px-2 py-1"
                    value={row.outHours}
                    placeholder="18.0"
                    onChange={(e) => updateRow(row.key, { outHours: e.target.value })}
                  />
                </td>
                <td className="px-3 py-2">
                  <input
                    className="w-16 rounded border border-slate-700 bg-slate-800 px-2 py-1"
                    value={row.hourlyRate}
                    onChange={(e) => updateRow(row.key, { hourlyRate: e.target.value })}
                  />
                </td>
                <td className="px-3 py-2">
                  <button
                    type="button"
                    className="text-xs text-red-400 hover:underline"
                    onClick={() => removeRow(row.key)}
                  >
                    remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="button"
          className="rounded bg-slate-800 px-3 py-1.5 text-sm hover:bg-slate-700"
          onClick={() => setRows((rs) => [...rs, newRow()])}
        >
          + Add day
        </button>
        <button
          type="button"
          disabled={!canRun || simulateMutation.isPending}
          className="rounded bg-sky-600 px-4 py-1.5 text-sm font-medium hover:bg-sky-500 disabled:opacity-50"
          onClick={() => simulateMutation.mutate()}
        >
          {simulateMutation.isPending ? "Running..." : "Run Simulation"}
        </button>
        <button
          type="button"
          disabled={!results || saveMutation.isPending}
          className="rounded border border-slate-600 px-4 py-1.5 text-sm font-medium hover:bg-slate-800 disabled:opacity-50"
          onClick={() => saveMutation.mutate()}
        >
          {saveMutation.isPending ? "Saving..." : "Save to Reports"}
        </button>
        {saveMessage && <span className="text-sm text-emerald-400">{saveMessage}</span>}
      </div>

      {simulateMutation.isError && (
        <p className="text-sm text-red-400">{(simulateMutation.error as Error).message}</p>
      )}

      {results && (
        <div className="overflow-x-auto rounded border border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-900 text-left text-slate-400">
              <tr>
                <th className="px-3 py-2">Day</th>
                <th className="px-3 py-2">Meal Penalty Amount</th>
                <th className="px-3 py-2">Total Paid Amount</th>
                <th className="px-3 py-2">Description</th>
              </tr>
            </thead>
            <tbody>
              {results.map((r) => (
                <Fragment key={r.label}>
                  <tr className="cursor-pointer border-t border-slate-800 hover:bg-slate-900" onClick={() => toggleExpanded(r.label)}>
                    <td className="px-3 py-2 font-medium">
                      {expanded.has(r.label) ? "▾" : "▸"} {r.label}
                    </td>
                    <td className="px-3 py-2">${r.totalPenalty.toFixed(2)}</td>
                    <td className="px-3 py-2 font-medium text-emerald-400">${r.totalPaidAmount.toFixed(2)}</td>
                    <td className="px-3 py-2 text-slate-400">
                      {r.triggers.length === 0
                        ? "No meal breaks exceeded the trigger thresholds."
                        : `${r.triggers.length} row(s) triggered across ${r.windows.length} window(s).`}
                    </td>
                  </tr>
                  {expanded.has(r.label) && (
                    <tr className="border-t border-slate-800 bg-slate-900/50">
                      <td colSpan={4} className="px-3 py-3">
                        <p className="mb-2 text-xs text-slate-400">
                          Paid Hours = {r.paidHours.toFixed(2)}h × ${r.hourlyRate.toFixed(2)}/h wages = $
                          {(r.paidHours * r.hourlyRate).toFixed(2)}, + ${r.totalPenalty.toFixed(2)} penalty = $
                          {r.totalPaidAmount.toFixed(2)} total paid. Windows:{" "}
                          {r.windows.map((w) => `[${w.start}→${w.end} = ${w.length.toFixed(2)}h]`).join(", ")}
                        </p>
                        {r.triggers.length > 0 && (
                          <ul className="list-inside list-disc space-y-1 text-xs text-slate-300">
                            {r.triggers.map((t, i) => (
                              <li key={i}>{t.description}</li>
                            ))}
                          </ul>
                        )}
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
