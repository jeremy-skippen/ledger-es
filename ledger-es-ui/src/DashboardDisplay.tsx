import { currencyFormat } from "./config";
import { Dashboard } from "./dashboard";
import "./DashboardDisplay.css";

interface DashboardProps {
  dashboard?: Dashboard;
}

export default function DashboardDisplay({ dashboard }: DashboardProps) {
  return (
    <section className="dashboard-root">
      <div className="dashboard-section" style={{ flexBasis: "30%" }}>
        <h2>Ledgers</h2>
        <div className="dashboard-hero-number">
          {dashboard?.ledgerCount ?? 0}
        </div>
        <div className="dashboard-sub-number">
          {dashboard?.ledgerOpenCount ?? 0} ledgers opened
        </div>
        <div className="dashboard-sub-number">
          {dashboard?.ledgerClosedCount ?? 0} ledgers closed
        </div>
      </div>
      <div className="dashboard-section" style={{ flexBasis: "30%" }}>
        <h2>Transactions</h2>
        <div className="dashboard-hero-number">
          {dashboard?.transactionCount ?? 0}
        </div>
        <div className="dashboard-sub-number">
          {dashboard?.receiptCount ?? 0} receipts
        </div>
        <div className="dashboard-sub-number">
          {dashboard?.paymentCount ?? 0} payments
        </div>
      </div>
      <div className="dashboard-section" style={{ flexBasis: "40%" }}>
        <h2>Cash</h2>
        <div className="dashboard-hero-number">
          {currencyFormat.format(dashboard?.netAmount ?? 0)}
        </div>
        <div className="dashboard-sub-number">
          {currencyFormat.format(dashboard?.receiptAmount ?? 0)} cash in
        </div>
        <div className="dashboard-sub-number">
          {currencyFormat.format(dashboard?.paymentAmount ?? 0)} cash out
        </div>
      </div>
    </section>
  );
}
