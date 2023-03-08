import * as signalR from "@microsoft/signalr";
import { useEffect, useMemo, useState } from "react";
import { currencyFormat, dateTimeFormat } from "./config";
import { Ledger, getLedger } from "./ledger";
import Modal, { ModalBody, ModalHeader } from "./Modal";
import "./LedgerDetailDisplay.css";

export interface LedgerDetailDisplayProps {
  ledgerId: string;
  onClose: () => void;
}

export default function LedgerDetailDisplay({
  ledgerId,
  onClose,
}: LedgerDetailDisplayProps) {
  const [ledger, setLedger] = useState<Ledger>();
  const entries = useMemo(() => {
    const reversed = ledger ? [...ledger.entries] : [];
    reversed.reverse();
    return reversed;
  }, [ledger]);

  useEffect(() => {
    getLedger(ledgerId).then(setLedger);
  }, [ledgerId]);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`http://localhost:8082/signalr/ledger?ledgerId=${ledgerId}`, {
        withCredentials: false,
      })
      .build();

    conn.on("LedgerUpdated", (ledger: Ledger) => setLedger(ledger));
    conn.start();

    return () => {
      conn.stop();
    };
  }, [ledgerId]);

  return (
    <Modal>
      {ledger ? (
        <>
          <ModalHeader>
            <button className="close" type="button" onClick={onClose}>
              &times;
            </button>
            <h2>
              {ledger.ledgerName} - {currencyFormat.format(ledger.balance)}
            </h2>
          </ModalHeader>
          <ModalBody>
            {entries.length > 0 ? (
              entries.map((entry) => (
                <div
                  key={entry.entryId}
                  className={`ledger-entry ${entry.type}`}
                >
                  <div className="description">{entry.description}</div>
                  <div className="amount">
                    {currencyFormat.format(entry.amount)}
                  </div>
                  <div className="meta">
                    {entry.type} journalled{" "}
                    {dateTimeFormat.format(new Date(entry.journalDate))}
                  </div>
                </div>
              ))
            ) : (
              <div>No ledger entries</div>
            )}
          </ModalBody>
        </>
      ) : (
        <>Loading...</>
      )}
    </Modal>
  );
}
