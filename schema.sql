CREATE DATABASE FolioIQ;
GO
USE FolioIQ;
GO

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Portfolios (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CashBalance DECIMAL(18,2) NOT NULL DEFAULT 10000.00,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Holdings (
    Id INT PRIMARY KEY IDENTITY,
    PortfolioId INT NOT NULL FOREIGN KEY REFERENCES Portfolios(Id),
    Ticker NVARCHAR(10) NOT NULL,
    CompanyName NVARCHAR(255) NOT NULL,
    Shares DECIMAL(18,6) NOT NULL,
    AverageBuyPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT UQ_Holdings_Portfolio_Ticker UNIQUE(PortfolioId, Ticker)
);

CREATE TABLE Transactions (
    Id INT PRIMARY KEY IDENTITY,
    PortfolioId INT NOT NULL FOREIGN KEY REFERENCES Portfolios(Id),
    Ticker NVARCHAR(10) NOT NULL,
    CompanyName NVARCHAR(255) NOT NULL,
    TransactionType NVARCHAR(4) NOT NULL CHECK (TransactionType IN ('BUY','SELL')),
    Shares DECIMAL(18,6) NOT NULL,
    PricePerShare DECIMAL(18,2) NOT NULL,
    TotalValue DECIMAL(18,2) NOT NULL,
    ExecutedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE PortfolioSnapshots (
    Id INT PRIMARY KEY IDENTITY,
    PortfolioId INT NOT NULL FOREIGN KEY REFERENCES Portfolios(Id),
    TotalValue DECIMAL(18,2) NOT NULL,
    SnapshotDate DATE NOT NULL,
    CONSTRAINT UQ_Snapshot_Portfolio_Date UNIQUE(PortfolioId, SnapshotDate)
);

CREATE TABLE Watchlist (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Ticker NVARCHAR(10) NOT NULL,
    CompanyName NVARCHAR(255) NOT NULL,
    AddedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Watchlist_User_Ticker UNIQUE(UserId, Ticker)
);

// working on wire up database init on startup...