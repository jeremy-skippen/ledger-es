import * as signalR from "@microsoft/signalr";
import { useEffect, useState } from "react";
import DashboardDisplay from "./DashboardDisplay";
import { Dashboard, getDashboard } from "./dashboard";
import LedgerDetailDisplay from "./LedgerDetailDisplay";
import LedgerListDisplay from "./LedgerListDisplay";
import "./App.css";

export default function App() {
  const [dashboard, setDashboard] = useState<Dashboard>();
  const [selectedLedgerId, setSelectedLedgerId] = useState<string>();

  useEffect(() => {
    getDashboard().then((d) => setDashboard(d));
  }, []);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:8082/signalr/dashboard", {
        withCredentials: false,
      })
      .build();

    conn.on("DashboardUpdated", (dashboard: Dashboard) =>
      setDashboard(dashboard)
    );
    conn.start();

    return () => {
      conn.stop();
    };
  }, []);

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
