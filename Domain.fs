namespace FolioIQ

open WebSharper

[<JavaScript>]
type UserSession = {
    UserId: int
    Username: string
    Email: string
}

[<JavaScript>]
type RegisterRequest = {
    Email: string
    Username: string
    Password: string
}

[<JavaScript>]
type LoginRequest = {
    Email: string
    Password: string
}

[<JavaScript>]
type Holding = {
    Ticker: string
    CompanyName: string
    Shares: float
    AverageBuyPrice: float
    CurrentPrice: float
    CurrentValue: float
    ProfitLoss: float
    ProfitLossPct: float
}

[<JavaScript>]
type Transaction = {
    Id: int
    Ticker: string
    CompanyName: string
    TransactionType: string
    Shares: float
    PricePerShare: float
    TotalValue: float
    ExecutedAt: string
}

[<JavaScript>]
type Portfolio = {
    CashBalance: float
    TotalInvested: float
    TotalCurrentValue: float
    TotalValue: float
    TotalProfitLoss: float
    TotalProfitLossPct: float
    Holdings: Holding list
}

[<JavaScript>]
type StockQuote = {
    Ticker: string
    CompanyName: string
    Price: float
    Change: float
    ChangePct: float
    Volume: int64
}

[<JavaScript>]
type StockSearchResult = {
    Ticker: string
    CompanyName: string
    Region: string
    Currency: string
}

[<JavaScript>]
type TradeRequest = {
    Ticker: string
    CompanyName: string
    Shares: float
    TradeType: string
}

[<JavaScript>]
type TradeResult =
    | Success of string
    | Failure of string

[<JavaScript>]
type LeaderboardEntry = {
    Rank: int
    Username: string
    TotalValue: float
    ProfitLossPct: float
}

[<JavaScript>]
type WatchlistItem = {
    Ticker: string
    CompanyName: string
}

// working on add watchlist add and remove endpoints...