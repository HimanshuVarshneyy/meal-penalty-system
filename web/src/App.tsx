import { NavLink, Route, Routes } from "react-router-dom";
import { ActorNameInput } from "./components/ActorNameInput";
import { AuditPage } from "./pages/Audit/AuditPage";
import { ReportsPage } from "./pages/Reports/ReportsPage";
import { RulesPage } from "./pages/Rules/RulesPage";
import { SimulatePage } from "./pages/Simulate/SimulatePage";

const navItems = [
  { to: "/simulate", label: "Simulation" },
  { to: "/rules", label: "Rules" },
  { to: "/reports", label: "Reports" },
  { to: "/audit", label: "Audit" },
];

function App() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-4 py-3">
          <div className="flex items-center gap-6">
            <h1 className="text-lg font-semibold">Meal Penalty System</h1>
            <nav className="flex gap-1">
              {navItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `rounded px-3 py-1.5 text-sm font-medium transition-colors ${
                      isActive
                        ? "bg-sky-600 text-white"
                        : "text-slate-300 hover:bg-slate-800 hover:text-white"
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <ActorNameInput />
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6">
        <Routes>
          <Route path="/" element={<SimulatePage />} />
          <Route path="/simulate" element={<SimulatePage />} />
          <Route path="/rules" element={<RulesPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/audit" element={<AuditPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
