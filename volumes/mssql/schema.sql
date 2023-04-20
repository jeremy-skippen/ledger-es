USE [master]
GO

IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE [name] = 'ledger-es')
BEGIN
    CREATE DATABASE [ledger-es];
END;
GO

USE [ledger-es];
GO

IF OBJECT_ID('dbo.ProjectionPosition', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectionPosition(
        ProjectionName NVARCHAR(50) NOT NULL,
        StreamPosition BIGINT NOT NULL,

        CONSTRAINT [PK_ProjectionPosition] PRIMARY KEY CLUSTERED (ProjectionName)
    );
END;
GO

IF OBJECT_ID('dbo.LedgerView', 'U') IS NULL
BEGIN
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
END;
GO

IF OBJECT_ID('dbo.DashboardView', 'U') IS NULL
BEGIN
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
END;
GO
