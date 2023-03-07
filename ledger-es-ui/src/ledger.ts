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
  const response = await fetch(`http://localhost:8082/api/ledger?page=${page}`);

  return response.json() as Promise<LedgerList>;
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
  const resposne = await fetch(`http://localhost:8082/api/ledger/${ledgerId}`);

  return resposne.json() as Promise<Ledger>;
}

export interface OpenLedgerRequest {
  ledgerId: string;
  ledgerName: string;
}

export type OpenLedgerResponse = OpenLedgerRequest;

export async function openLedger(
  ledger: OpenLedgerRequest
): Promise<OpenLedgerResponse | ProblemDetails> {
  const response = await fetch("http://localhost:8082/api/ledger", {
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
