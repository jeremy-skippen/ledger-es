import { useEffect, useMemo, useState } from "react";
import { currencyFormat, dateTimeFormat } from "./config";
import { LedgerList, getLedgerList } from "./ledger";
import AddLedgerModal from "./AddLedgerModal";
import "./LedgerListDisplay.css";

export interface LedgerListDisplayProps {
  onSelectLedger: (ledgerId: string) => void;
}

export default function LedgerListDisplay({
  onSelectLedger,
}: LedgerListDisplayProps) {
  const [showAddModal, setShowAddModal] = useState<boolean>(false);
  const [page, setPage] = useState<number>(0);
  const [showClosed, setShowClosed] = useState<boolean>(false);
  const [ledgerList, setLedgerList] = useState<LedgerList>();
  const totalPages = useMemo(() => {
    if (ledgerList) {
      return Math.ceil(ledgerList.totalCount / ledgerList.pageSize);
    }
    return 0;
  }, [ledgerList]);
  const allowPrev = page > 0;
  const allowNext = page < totalPages - 1;

  useEffect(() => {
    getLedgerList(page).then((ll) => setLedgerList(ll));
  }, [page]);

  return (
    <>
      {ledgerList?.results.length ? (
        <>
          <div className="ledger-header">
            <div className="total">{ledgerList.totalCount} total ledgers</div>
            <button type="button" onClick={() => setShowAddModal(true)}>
              Add Ledger
            </button>
          </div>
          <table className="ledger-list">
            <thead>
              <tr>
                <th className="hide-on-mobile" style={{ width: "5rem" }}>
                  Id
                </th>
                <th>Name</th>
                <th style={{ width: "10rem" }}>Balance</th>
                <th className="hide-on-mobile" style={{ width: "12rem" }}>
                  Last Update
                </th>
              </tr>
            </thead>
            <tbody>
              {ledgerList.results.map((i) => (
                <tr
                  key={i.ledgerId}
                  className={i.isOpen ? "open" : "closed"}
                  style={{
                    display: !i.isOpen && !showClosed ? "none" : undefined,
                  }}
                >
                  <td className="hide-on-mobile">
                    <abbr title={i.ledgerId}>{i.ledgerId.substring(0, 8)}</abbr>
                  </td>
                  <td>
                    <a onClick={() => onSelectLedger(i.ledgerId)}>
                      {i.ledgerName}
                    </a>
                  </td>
                  <td>{currencyFormat.format(i.balance)}</td>
                  <td className="hide-on-mobile">
                    {dateTimeFormat.format(new Date(i.modifiedDate))}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr>
                <td colSpan={4}>
                  <div className="pagination">
                    <button
                      type="button"
                      disabled={!allowPrev}
                      className="hide-on-mobile"
                      onClick={() => setPage(0)}
                    >
                      ⇤
                    </button>
                    <button
                      type="button"
                      disabled={!allowPrev}
                      onClick={() => setPage(page - 1)}
                    >
                      ←
                    </button>
                    <div className="pagination-display">
                      Page {page + 1} of {totalPages}
                    </div>
                    <button
                      type="button"
                      disabled={!allowNext}
                      onClick={() => setPage(page + 1)}
                    >
                      →
                    </button>
                    <button
                      type="button"
                      disabled={!allowNext}
                      className="hide-on-mobile"
                      onClick={() => setPage(totalPages - 1)}
                    >
                      ⇥
                    </button>
                  </div>
                </td>
              </tr>
            </tfoot>
          </table>
        </>
      ) : (
        <div>No ledgers found</div>
      )}
      {showAddModal && (
        <AddLedgerModal onClose={() => setShowAddModal(false)} />
      )}
    </>
  );
}
