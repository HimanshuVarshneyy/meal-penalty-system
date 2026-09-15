import { useQuery } from "@tanstack/react-query";
import { Fragment, useState } from "react";
import { api } from "../../api/client";

export function ReportsPage() {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [label, setLabel] = useState("");
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const runsQuery = useQuery({
    queryKey: ["reports", from, to, label],
    queryFn: () => api.listReports({ from: from || undefined, to: to || undefined, label: label || undefined }),
  });

  const detailQuery = useQuery({
    queryKey: ["report", expandedId],
    queryFn: () => api.getReport(expandedId!),
    enabled: expandedId != null,
  });

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold">Reports</h2>

      <div className="flex flex-wrap items-end gap-3 rounded border border-slate-800 bg-slate-900 p-3">
        <label className="text-sm text-slate-300">
          From
          <input type="date" className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="text-sm text-slate-300">
          To
          <input type="date" className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <label className="text-sm text-slate-300">
          Label contains
          <input className="mt-1 block rounded border border-slate-700 bg-slate-800 px-2 py-1" value={label} onChange={(e) => setLabel(e.target.value)} placeholder="e.g. Mon" />
        </label>
      </div>

      <div className="overflow-x-auto rounded border border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-900 text-left text-slate-400">
            <tr>
              <th className="px-3 py-2">Day</th>
              <th className="px-3 py-2">Work date</th>
              <th className="px-3 py-2">Paid hours</th>
              <th className="px-3 py-2">Meal Penalty Amount</th>
              <th className="px-3 py-2">Total Paid Amount</th>
              <th className="px-3 py-2">Saved by</th>
              <th className="px-3 py-2">Saved at</th>
            </tr>
          </thead>
          <tbody>
            {runsQuery.data?.map((run) => (
              <Fragment key={run.id}>
                <tr
                  className="cursor-pointer border-t border-slate-800 hover:bg-slate-900"
                  onClick={() => setExpandedId(expandedId === run.id ? null : run.id)}
                >
                  <td className="px-3 py-2 font-medium">
                    {expandedId === run.id ? "▾" : "▸"} {run.label}
                  </td>
                  <td className="px-3 py-2">{run.workDate}</td>
                  <td className="px-3 py-2">{run.paidHours.toFixed(2)}</td>
                  <td className="px-3 py-2">${run.totalPenalty.toFixed(2)}</td>
                  <td className="px-3 py-2 font-medium text-emerald-400">${run.totalPaidAmount.toFixed(2)}</td>
                  <td className="px-3 py-2 text-slate-400">{run.createdBy}</td>
                  <td className="px-3 py-2 text-slate-400">{new Date(run.createdAt).toLocaleString()}</td>
                </tr>
                {expandedId === run.id && (
                  <tr className="border-t border-slate-800 bg-slate-900/50">
                    <td colSpan={7} className="px-3 py-3">
                      {detailQuery.isLoading ? (
                        <span className="text-xs text-slate-400">Loading...</span>
                      ) : detailQuery.data && detailQuery.data.details.length > 0 ? (
                        <ul className="list-inside list-disc space-y-1 text-xs text-slate-300">
                          {detailQuery.data.details.map((d) => (
                            <li key={d.id}>{d.description}</li>
                          ))}
                        </ul>
                      ) : (
                        <span className="text-xs text-slate-400">No meal penalty rows triggered this day.</span>
                      )}
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {runsQuery.data?.length === 0 && (
              <tr>
                <td colSpan={7} className="px-3 py-6 text-center text-slate-500">
                  No saved runs yet. Save a simulation from the Simulation page.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
