export interface SimMealRow {
  mealStart: string;
  mealEnd: string;
}

export interface SimRow {
  key: string;
  label: string;
  workDate: string;
  inHours: string;
  outHours: string;
  hourlyRate: string;
  meals: SimMealRow[];
}

let nextKey = 1;
export function newRow(overrides: Partial<SimRow> = {}): SimRow {
  return {
    key: `row-${nextKey++}`,
    label: "",
    workDate: "",
    inHours: "",
    outHours: "",
    hourlyRate: "40",
    meals: [{ mealStart: "", mealEnd: "" }],
    ...overrides,
  };
}

export const sampleRows: SimRow[] = [
  newRow({ label: "Mon", workDate: "2026-09-14", inHours: "8.0", outHours: "18.0", hourlyRate: "40", meals: [{ mealStart: "13.0", mealEnd: "14.0" }] }),
  newRow({ label: "Tue", workDate: "2026-09-15", inHours: "8.0", outHours: "20.0", hourlyRate: "40", meals: [{ mealStart: "15.2", mealEnd: "15.7" }] }),
  newRow({ label: "Wed", workDate: "2026-09-16", inHours: "6.0", outHours: "16.0", hourlyRate: "40", meals: [{ mealStart: "", mealEnd: "" }] }),
];
