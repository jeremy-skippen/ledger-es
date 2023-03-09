import { useState } from "react";
import DashboardDisplay from "./DashboardDisplay";
import { Dashboard, useDashboardLiveUpdate } from "./dashboard";
import LedgerDetailDisplay from "./LedgerDetailDisplay";
import LedgerListDisplay from "./LedgerListDisplay";
import "./App.css";

export default function App() {
  const [dashboard, setDashboard] = useState<Dashboard>();
  const [selectedLedgerId, setSelectedLedgerId] = useState<string>();

  useDashboardLiveUpdate(setDashboard);

  return (
    <>
      <DashboardDisplay dashboard={dashboard} />
      <main>
        <LedgerListDisplay onSelectLedger={setSelectedLedgerId} />
        {selectedLedgerId && (
          <LedgerDetailDisplay
            ledgerId={selectedLedgerId}
            onClose={() => setSelectedLedgerId(undefined)}
          />
        )}
      </main>
    </>
  );
}
