CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Username TEXT NOT NULL UNIQUE,
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Portfolios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    CashBalance REAL NOT NULL DEFAULT 10000.0,
    CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Holdings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    Ticker TEXT NOT NULL,
    CompanyName TEXT NOT NULL,
    Shares REAL NOT NULL,
    AverageBuyPrice REAL NOT NULL,
    UNIQUE(UserId, Ticker)
);

CREATE TABLE IF NOT EXISTS Transactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    Ticker TEXT NOT NULL,
    CompanyName TEXT NOT NULL,
    TransactionType TEXT NOT NULL CHECK (TransactionType IN ('BUY','SELL')),
    Shares REAL NOT NULL,
    PricePerShare REAL NOT NULL,
    TotalValue REAL NOT NULL,
    ExecutedAt TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Watchlist (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(Id),
    Ticker TEXT NOT NULL,
    CompanyName TEXT NOT NULL,
    AddedAt TEXT DEFAULT (datetime('now')),
    UNIQUE(UserId, Ticker)
);
