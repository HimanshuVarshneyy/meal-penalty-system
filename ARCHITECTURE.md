# Meal Penalty System — Architecture

## 1. What this is

A payroll add-on that computes **meal penalties** owed to film-production crew when they aren't
given a meal break on time. Penalty rules (thresholds, amounts, intervals) are configurable and
change over time, so the system is split into a data-driven **rule engine** (backend) and a UI for
simulating timesheets, reviewing saved results, editing rules, and auditing every rule change.

```
/multi-container-app
  /api    .NET 10 solution — Domain / Application / Infrastructure / Api / Tests
  /web    React 18 + Vite + TypeScript + Tailwind
```

The two are independent processes talking over HTTP:

```
┌─────────────────────┐        HTTP/JSON        ┌──────────────────────────┐        Dapper/SQL        ┌──────────────────┐
│  web (Vite, :5173)  │ ───────────────────────▶ │  api (Kestrel, :5080)    │ ────────────────────────▶ │ mealpenalty.db   │
│  React pages        │ ◀─────────────────────── │  MealPenalty.Api         │ ◀──────────────────────── │ (file-backed      │
└─────────────────────┘   CORS-allowed, /api/v1  └──────────────────────────┘                           │  SQLite)          │
                                                                                                          └──────────────────┘
```

---

## 2. Backend — layered, so rule/timesheet logic can change independently

Five projects, each only depending on the ones to its left:

```
MealPenalty.Domain  ◀── MealPenalty.Application  ◀── MealPenalty.Infrastructure  ◀── MealPenalty.Api
      ▲                         ▲                                                        │
      └─────────────────────────┴──────────────── MealPenalty.Tests ──────────────────────┘
```

### MealPenalty.Domain
Plain POCOs and enums only — no logic, no dependencies. `TimesheetEntry`, `MealBreak`,
`PenaltyRuleSet`, `PenaltyRule`, `PenaltyType` (enum: `Hour`/`Amount`/`Daily`), `Window`,
`PenaltyTriggerResult`, `DayResult`, `AuditAction`, `PenaltyRuleAudit`, `CalculationRun` +
`CalculationRunDetail`.

### MealPenalty.Application
The calculation engine and business services, built as two swappable strategy points so *new*
rule/timesheet behavior never requires touching the orchestrator:

- **`IWindowSplitter`** (`Windows/`) — turns a `TimesheetEntry` (In/Out + N meal breaks) into a
  list of `Window`s: `In→Meal1Start`, `Meal1End→Meal2Start`, …, `LastMealEnd→Out`. Each meal resets
  the penalty timer, so every window is evaluated independently. `DefaultWindowSplitter` is the one
  implementation today; a different timesheet shape (e.g. a jurisdiction with different splitting
  rules) plugs in as a new implementation.
- **`IPenaltyTypeHandler`** (`PenaltyTypes/`) — prices one trigger of a rule row. One class per
  `PenaltyType`: `HourRateHandler` (value × hourly rate), `FlatAmountHandler` (flat $ value),
  `DailyPercentHandler` (value% × 8×hourly-rate). Adding a new penalty type is one new class + one
  enum value — the engine never changes.
- **`PenaltyCalculationEngine`** (`Engine/`) — orchestrates: split into windows → for each window,
  evaluate every active rule row (`window.Length > rule.StartHours` to trigger; counted hours are
  capped by `EndHours`; trigger count is 1 with no interval, else `ceil(counted / interval)`) →
  price each trigger via the matching handler → sum into a `DayResult` (paid hours, total penalty,
  total paid amount, and the full per-window/per-trigger trace used for the UI's "how this was
  computed" breakdown).
- **`RuleSetService`** (`Services/`) — owns every write to a rule set. A rule set that `IsActive`
  is immutable; editing means creating a new version. Every create/update/activate writes exactly
  one `PenaltyRuleAudit` row alongside the change.
- **`SimulationService`** (`Services/`) — runs the engine for a batch of timesheet entries against
  a rule set, either ephemeral (`/simulate`) or persisted (`/simulate/save`, which also writes a
  `TimesheetEntry` + `CalculationRun`).
- **`Abstractions/`** — repository interfaces (`IRuleSetRepository`, `ITimesheetRepository`,
  `ICalculationRunRepository`, `IAuditRepository`) that Infrastructure implements. This is the
  seam that lets the storage engine change without touching business logic.

### MealPenalty.Infrastructure
Dapper repositories against SQLite. `SqliteConnectionFactory` opens a fresh connection per call
(same pattern you'd use against a real server DB); see §4 for why SQLite specifically and how the
file survives restarts. `SchemaInitializer` creates the schema (`CREATE TABLE IF NOT EXISTS`) and
seeds one sample rule set on first run, plus runs small `ALTER TABLE ADD COLUMN` migrations for
columns added after a given `.db` file was first created.

### MealPenalty.Api
ASP.NET Core controllers + Swagger + CORS (allows the Vite dev origin). Thin — maps HTTP to
Application services/repositories, translating domain objects to/from request/response DTOs
(`Dtos/`). See §5 for the endpoint list.

### MealPenalty.Tests
xUnit regression tests for the engine, built directly from the hand-worked examples in the original
spec (hourly rate $40; rows 6-7h/AMOUNT $25, 7-9h/HOUR 0.5h per 0.5h, 9-24h/DAILY 10% per 1h) —
Monday = $0 penalty, Tuesday = $45, Wednesday = $137, each asserted against the engine's output.

---

## 3. Frontend — React 18 + Vite + TypeScript + Tailwind

```
web/src/
  api/        client.ts (typed fetch wrapper), types.ts (API contracts), actor.ts (localStorage)
  lib/        ruleDiff.ts (pure diff algorithm for rule versions)
  components/ ActorNameInput.tsx, RuleDiffView.tsx (shared across pages)
  pages/
    Simulate/  editable timesheet grid → POST /simulate or /simulate/save
    Rules/     rule-set list + editor, versioning, diff-before-save
    Reports/   saved CalculationRuns, filterable, drill-down detail
    Audit/     PenaltyRuleAudit trail, human-readable diff instead of raw JSON
  App.tsx      nav shell + react-router routes
  main.tsx     QueryClientProvider (TanStack Query) + BrowserRouter
```

- **State**: TanStack Query owns all server state (caching, invalidation after mutations); no
  global client-state store — each page keeps its own local UI state.
- **Actor identity**: a plain "Your name" field, persisted to `localStorage`, sent as the
  `X-Actor-Name` header on every write. There's no auth in this POC — this header is how
  `ChangedBy`/`CreatedBy` get populated.
- **Rule diffing** (`lib/ruleDiff.ts`): rows are compared positionally (row *N* before vs. row *N*
  after) to produce added/removed/changed entries with per-field before→after values. Used live in
  the Rules page (blocks saving a no-op version) and to render the Audit page's diff column instead
  of raw JSON.

---

## 4. Database

**Engine**: SQLite, file-backed at `api/MealPenalty.Api/Data/mealpenalty.db` (connection string
`Data Source=<path>`, overridable via `ConnectionStrings:MealPenaltyDb`). Chosen so Dapper is used
exactly as it would be against a real server (parameterized SQL, transactions, a connection
per call) — swapping to SQL Server/Postgres later is a connection-string + minor type change, not a
rewrite. Data persists across app restarts; it does **not** require any server process, just the
one file.

### Entity-relationship diagram

```mermaid
erDiagram
    PenaltyRuleSet ||--o{ PenaltyRule : "has rows"
    PenaltyRuleSet ||--o{ PenaltyRuleAudit : "change history"
    PenaltyRuleSet ||--o{ CalculationRun : "used by"
    PenaltyRule ..o{ CalculationRunDetail : "referenced by (RuleId, nullable)"
    TimesheetEntry ||--o{ TimesheetMeal : "has meal breaks"
    TimesheetEntry ||--o{ CalculationRun : "produces"
    CalculationRun ||--o{ CalculationRunDetail : "has trigger lines"

    PenaltyRuleSet {
        int Id PK
        string Name
        bool IsActive
        datetime EffectiveFrom
        datetime CreatedAt
        string CreatedBy
    }
    PenaltyRule {
        int Id PK
        int RuleSetId FK
        int RowOrder
        decimal StartHours
        decimal EndHours
        string PenaltyType
        decimal Value
        decimal IntervalHours "nullable"
    }
    PenaltyRuleAudit {
        int Id PK
        int RuleSetId FK
        int RuleId "nullable, not enforced FK"
        string Action
        string ChangedBy
        datetime ChangedAt
        string OldValueJson "nullable"
        string NewValueJson "nullable"
        string Reason "nullable"
    }
    TimesheetEntry {
        int Id PK
        string Label
        date WorkDate
        decimal InHours
        decimal OutHours
        decimal HourlyRate
        datetime CreatedAt
    }
    TimesheetMeal {
        int Id PK
        int TimesheetEntryId FK
        int SeqNo
        decimal MealStart
        decimal MealEnd
    }
    CalculationRun {
        int Id PK
        int TimesheetEntryId FK
        int RuleSetId FK
        string Label
        date WorkDate
        decimal HourlyRate
        decimal PaidHours
        decimal TotalPenalty
        decimal TotalPaidAmount
        datetime CreatedAt
        string CreatedBy
    }
    CalculationRunDetail {
        int Id PK
        int CalculationRunId FK
        int WindowIndex
        decimal WindowStart
        decimal WindowEnd
        int RuleId "nullable, not enforced FK"
        int TriggerCount
        decimal PenaltyAmount
        string Description
    }
```

### Tables, in plain terms

| Table | Purpose | Key relationships |
|---|---|---|
| **PenaltyRuleSet** | One row per *version* of the rule configuration. Only one row can have `IsActive = 1` at a time. Rule sets are never mutated once active — a new version is a new row, so historical calculations stay reproducible against the rules that were live when they ran. | Parent of `PenaltyRule`, `PenaltyRuleAudit`; referenced by `CalculationRun.RuleSetId`. |
| **PenaltyRule** | One row per rule "row" within a set (Start/End hours, Type, Value, Interval) — the 3-row sample table from the spec. `RowOrder` controls evaluation order within a rule set. | Child of `PenaltyRuleSet` via `RuleSetId`. Loosely referenced by `CalculationRunDetail.RuleId` and `PenaltyRuleAudit.RuleId` (not a DB-enforced FK — a rule can be deleted/superseded by a new version while old results still cite its id for traceability). |
| **PenaltyRuleAudit** | Append-only log of every Create/Update/Activate on a rule set — actor, timestamp, reason, and before/after JSON of the rule rows. No update/delete endpoint exists for this table by design. | References `PenaltyRuleSet.Id`. |
| **TimesheetEntry** | One row per day that was actually *saved* (via "Save to Reports" in the Simulation page) — In/Out/HourlyRate. Rows used only for an ephemeral `/simulate` preview are never written here. | Parent of `TimesheetMeal`; referenced by `CalculationRun.TimesheetEntryId`. |
| **TimesheetMeal** | One row per meal break on a saved timesheet entry (supports N meals/day, not just one), ordered by `SeqNo`. | Child of `TimesheetEntry`. |
| **CalculationRun** | One row per saved day's *result*: which rule set and timesheet entry produced it, paid hours, total penalty, and total paid amount (denormalized here rather than recomputed, so Reports doesn't need to re-run the engine or join back to `TimesheetEntry` for the hourly rate). | References `TimesheetEntry.Id` and `PenaltyRuleSet.Id`; parent of `CalculationRunDetail`. |
| **CalculationRunDetail** | One row per rule trigger that fired for that day (which window, which rule, how many times, the dollar amount, and a human-readable description) — this is what the Reports/Simulation "Description" column and expandable trace are built from. | Child of `CalculationRun`. |

**Why `CalculationRunDetail.RuleId` and `PenaltyRuleAudit.RuleId` aren't enforced foreign keys**:
both need to keep pointing at *the rule row that was actually used/changed* even after that rule
set version is superseded or a rule row is edited away — enforcing referential integrity there
would either cascade-delete history or block deleting old rule versions. They're informational,
not join-critical (the detail's `Description` text is self-contained).

---

## 5. API surface (`/api/v1`)

| Method & path | Purpose |
|---|---|
| `POST /simulate` | Run the engine against submitted timesheet rows + a rule set id. Nothing persisted. |
| `POST /simulate/save` | Same as above, but also persists a `TimesheetEntry` + `CalculationRun` (+ details) per day. Requires `X-Actor-Name`. |
| `GET /rulesets` | List all rule set versions (summary). |
| `GET /rulesets/{id}` | One rule set with its rows. |
| `GET /rulesets/active` | The currently active rule set. |
| `POST /rulesets` | Create a new rule set version. Requires `X-Actor-Name`; writes a `Create` audit row. |
| `PUT /rulesets/{id}/rules` | Replace the rows of a **non-active** rule set. 409 if the set is active. Writes an `Update` audit row. |
| `POST /rulesets/{id}/activate` | Make this version the active one (deactivates all others). Writes an `Activate` audit row. |
| `GET /reports` | List saved `CalculationRun`s, filterable by date range / label. |
| `GET /reports/{id}` | One saved run with its trigger-line details. |
| `GET /audit/rules` | The `PenaltyRuleAudit` trail, filterable by rule set / date range. |

---

## 6. Extensibility points (the "future-proofing" the original spec asked for)

- **New penalty type** (beyond Hour/Amount/Daily): add one `IPenaltyTypeHandler` implementation +
  one `PenaltyType` enum value. No engine, schema, or DTO changes required beyond the enum.
- **New timesheet-splitting rule** (e.g. a jurisdiction with different meal-window logic): add a
  new `IWindowSplitter` implementation and register it instead of `DefaultWindowSplitter`.
- **Rule changes over time**: handled entirely as data (new `PenaltyRuleSet` versions), not code —
  adjusting thresholds/amounts never requires a deploy.
- **New persisted field** (as happened with `TotalPaidAmount`): add to the `CREATE TABLE` for new
  databases *and* an `EnsureColumn`/`ALTER TABLE` call in `SchemaInitializer.Initialize` so existing
  `.db` files pick it up without losing data — see the migration pattern already in place there.
