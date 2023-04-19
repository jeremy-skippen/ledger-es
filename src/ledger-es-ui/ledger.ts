import * as signalR from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import { API_BASE_URI } from "./config";
import { ProblemDetails } from "./problem";

export interface LedgerList {
  results: LedgerListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface LedgerListItem {
  ledgerId: string;
  ledgerName: string;
  isOpen: boolean;
  balance: number;
  modifiedDate: string;
}

export async function getLedgerList(page: number = 0): Promise<LedgerList> {
  const response = await fetch(`${API_BASE_URI}/api/ledger?page=${page}`);

  return response.json() as Promise<LedgerList>;
}

export function useLedgerListLiveUpdate(
  page: number,
  ledgerList: LedgerList | undefined,
  onLedgerListUpdated: (ledgerList: LedgerList) => void
) {
  const ledgerListRef = useRef(ledgerList);

  // Fetch the ledger list page when the page changes
  useEffect(() => {
    getLedgerList(page).then((ll) => {
      console.debug("Retrieved ledger list", ll);
      onLedgerListUpdated(ll);
    });
  }, [page]);

  // Establish the signalR connection
  // Listen for new ledgers and append them to the ledger list if we're on
  // the last page, which we can tell if the result length < page size.
  const [conn, setConn] = useState<signalR.HubConnection>();
  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URI}/signalr/ledger`, {
        withCredentials: false,
      })
      .build();

    conn.on("LedgerAdded", (ledger: Ledger) => {
      console.debug("Ledger added", ledger);
      if (
        ledgerListRef.current &&
        ledgerListRef.current.results.length < ledgerListRef.current.pageSize
      ) {
        const ll = {
          ...ledgerListRef.current,
          results: [...ledgerListRef.current.results, ledger],
        };
        console.debug("Ledger list updated", ll);
        onLedgerListUpdated(ll);
      }
    });
    conn.on("LedgerUpdated", (ledger: Ledger) => {
      console.debug("Ledger updated", ledger);
      if (
        ledgerListRef.current &&
        ledgerListRef.current.results.some(
          (li) => li.ledgerId == ledger.ledgerId
        )
      ) {
        const ll = {
          ...ledgerListRef.current,
          results: ledgerListRef.current.results.map((li) =>
            li.ledgerId == ledger.ledgerId ? ledger : li
          ),
        };
        console.debug("Ledger list updated", ll);
        onLedgerListUpdated(ll);
      }
    });

    conn.start();

    setConn(conn);

    return () => {
      conn.stop();
    };
  }, []);

  // Listen for changes to any ledger in the ledger list
  const [watchLedgerIds, setWatchLedgerIds] = useState<string[]>([]);
  useEffect(() => {
    const updatedWatchLedgerIds =
      ledgerList?.results.map((l) => l.ledgerId) ?? [];
    const addIds = updatedWatchLedgerIds.filter(
      (id) => !watchLedgerIds.includes(id)
    );
    const removeIds = watchLedgerIds.filter(
      (id) => !updatedWatchLedgerIds.includes(id)
    );
    const promises: Promise<void>[] = [];

    if (conn && addIds.length) {
      promises.push(conn.invoke("ListenToLedgers", addIds));
    }
    if (conn && removeIds.length) {
      promises.push(conn.invoke("UnListenToLedgers", removeIds));
    }

    if (promises.length) {
      Promise.all(promises).then(() =>
        setWatchLedgerIds(updatedWatchLedgerIds)
      );
    }

    ledgerListRef.current = ledgerList;
  }, [ledgerList]);
}

export interface Ledger {
  ledgerId: string;
  ledgerName: string;
  isOpen: boolean;
  entries: LedgerItem[];
  balance: number;
  version: number;
  modifiedDate: string;
}

export interface LedgerItem {
  entryId: string;
  description: string;
  amount: number;
  type: "receipt" | "payment";
  journalDate: string;
}

export async function getLedger(ledgerId: string): Promise<Ledger> {
  const response = await fetch(`${API_BASE_URI}/api/ledger/${ledgerId}`);

  return response.json() as Promise<Ledger>;
}

export function useLedgerLiveUpdate(
  ledgerId: string,
  onLedgerUpdated: (ledger: Ledger) => void
) {
  useEffect(() => {
    // Get the initial ledger model
    getLedger(ledgerId).then(onLedgerUpdated);

    // Subscribe to updates to the model via signalR
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URI}/signalr/ledger?ledgerId=${ledgerId}`, {
        withCredentials: false,
      })
      .build();

    conn.on("LedgerUpdated", onLedgerUpdated);
    conn.start();

    return () => {
      conn.stop();
    };
  }, [ledgerId]);
}

export interface OpenLedgerRequest {
  ledgerId: string;
  ledgerName: string;
}

export type OpenLedgerResponse = OpenLedgerRequest;

export async function openLedger(
  ledger: OpenLedgerRequest
): Promise<OpenLedgerResponse | ProblemDetails> {
  const response = await fetch(`${API_BASE_URI}/api/ledger`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(ledger),
  });

  return response.ok
    ? (response.json() as Promise<OpenLedgerResponse>)
    : response
        .json()
        .then((problem) => Promise.reject<ProblemDetails>(problem));
}
