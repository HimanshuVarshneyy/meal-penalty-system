import { getActorName } from "./actor";
import type {
  CalculationRun,
  DayResult,
  RuleAuditEntry,
  RuleRow,
  RuleSetDetail,
  RuleSetSummary,
  SimulateSavedResult,
  TimesheetEntryInput,
} from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080/api/v1";

class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Actor-Name": getActorName(),
      ...options.headers,
    },
  });

  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new ApiError(body || `Request failed with status ${res.status}`, res.status);
  }

  if (res.status === 204) {
    return undefined as T;
  }

  return (await res.json()) as T;
}

export interface SimulateRequest {
  ruleSetId: number;
  entries: TimesheetEntryInput[];
}

export const api = {
  simulate: (payload: SimulateRequest) =>
    request<DayResult[]>("/simulate", { method: "POST", body: JSON.stringify(payload) }),

  simulateAndSave: (payload: SimulateRequest) =>
    request<SimulateSavedResult[]>("/simulate/save", { method: "POST", body: JSON.stringify(payload) }),

  listRuleSets: () => request<RuleSetSummary[]>("/rulesets"),

  getRuleSet: (id: number) => request<RuleSetDetail>(`/rulesets/${id}`),

  getActiveRuleSet: () => request<RuleSetDetail>("/rulesets/active"),

  createRuleSet: (payload: { name: string; rules: RuleRow[]; reason?: string }) =>
    request<RuleSetDetail>("/rulesets", { method: "POST", body: JSON.stringify(payload) }),

  replaceRules: (id: number, payload: { rules: RuleRow[]; reason?: string }) =>
    request<void>(`/rulesets/${id}/rules`, { method: "PUT", body: JSON.stringify(payload) }),

  activateRuleSet: (id: number, payload: { reason?: string }) =>
    request<void>(`/rulesets/${id}/activate`, { method: "POST", body: JSON.stringify(payload) }),

  listReports: (params: { from?: string; to?: string; label?: string } = {}) => {
    const qs = new URLSearchParams();
    if (params.from) qs.set("from", params.from);
    if (params.to) qs.set("to", params.to);
    if (params.label) qs.set("label", params.label);
    const suffix = qs.toString() ? `?${qs.toString()}` : "";
    return request<CalculationRun[]>(`/reports${suffix}`);
  },

  getReport: (id: number) => request<CalculationRun>(`/reports/${id}`),

  listRuleAudit: (params: { ruleSetId?: number; from?: string; to?: string } = {}) => {
    const qs = new URLSearchParams();
    if (params.ruleSetId) qs.set("ruleSetId", String(params.ruleSetId));
    if (params.from) qs.set("from", params.from);
    if (params.to) qs.set("to", params.to);
    const suffix = qs.toString() ? `?${qs.toString()}` : "";
    return request<RuleAuditEntry[]>(`/audit/rules${suffix}`);
  },
};

export { ApiError };
