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

export async function getDashboard() : Promise<Dashboard> {
    const dashboardResponse = await fetch("http://localhost:8082/api/dashboard");

    return dashboardResponse.json() as Promise<Dashboard>;
}
