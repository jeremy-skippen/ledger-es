import * as signalR from "@microsoft/signalr";
import { useEffect } from "react";
import { API_BASE_URI } from "./config";

export interface Dashboard {
  ledgerCount: number;
  ledgerOpenCount: number;
  ledgerClosedCount: number;
  transactionCount: number;
  receiptCount: number;
  paymentCount: number;
  netAmount: number;
  receiptAmount: number;
  paymentAmount: number;
  version: number;
  modifiedDate: string;
}

export async function getDashboard(): Promise<Dashboard> {
  const dashboardResponse = await fetch(`${API_BASE_URI}/api/dashboard`);

  return dashboardResponse.json() as Promise<Dashboard>;
}

export function useDashboardLiveUpdate(
  onDashboardUpdated: (dashboard: Dashboard) => void
) {
  useEffect(() => {
    // Get the initial dashboard
    getDashboard().then((d) => {
      console.debug("Retrieved dashboard", d);
      onDashboardUpdated(d);
    });

    // Listen for changes via signalR
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URI}/signalr/dashboard`, {
        withCredentials: false,
      })
      .build();

    conn.on("DashboardUpdated", (dashboard: Dashboard) => {
      console.debug("Dashboard updated", dashboard);
      onDashboardUpdated(dashboard);
    });
    conn.start();

    return () => {
      conn.stop();
    };
  }, []);
}
