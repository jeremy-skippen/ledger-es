CREATE TABLE dbo.ProjectionPosition(
    ProjectionName NVARCHAR(50) NOT NULL,
    StreamPosition BIGINT NOT NULL,

    CONSTRAINT [PK_ProjectionPosition] PRIMARY KEY CLUSTERED (ProjectionName)
);

CREATE TABLE dbo.LedgerView(
    Id INT NOT NULL IDENTITY,
    LedgerId UNIQUEIDENTIFIER NOT NULL,
    LedgerName NVARCHAR(255) NOT NULL,
    IsOpen BIT NOT NULL,
    Entries NVARCHAR(MAX) NOT NULL,
    Balance DECIMAL(18, 2) NOT NULL,
    [Version] BIGINT NOT NULL,
    ModifiedDate DATETIMEOFFSET NOT NULL,

    CONSTRAINT [PK_LedgerView] PRIMARY KEY CLUSTERED (Id)
);

CREATE UNIQUE NONCLUSTERED INDEX IX_LedgerView_LedgerId ON dbo.LedgerView(LedgerId);

CREATE TABLE dbo.DashboardView(
    LedgerCount INT NOT NULL,
    LedgerOpenCount INT NOT NULL,
    LedgerClosedCount INT NOT NULL,
    TransactionCount INT NOT NULL,
    ReceiptCount INT NOT NULL,
    PaymentCount INT NOT NULL,
    NetAmount DECIMAL(18, 2) NOT NULL,
    ReceiptAmount DECIMAL(18, 2) NOT NULL,
    PaymentAmount DECIMAL(18, 2) NOT NULL,
    [Version] BIGINT NOT NULL,
    ModifiedDate DATETIMEOFFSET NOT NULL
);
