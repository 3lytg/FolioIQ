namespace FolioIQ

open System
open Microsoft.Data.Sqlite

module Database =

    let private connectionString = "Data Source=folioiq.db"

    let init () =
        use conn = new SqliteConnection(connectionString)
        conn.Open()
        let schema = System.IO.File.ReadAllText("schema.sql")
        let cmd = new SqliteCommand(schema, conn)
        cmd.ExecuteNonQuery() |> ignore

    let registerUser (req: RegisterRequest) : Async<Result<int, string>> =
        async {
            try
                use conn = new SqliteConnection(connectionString)
                conn.Open()
                let hash = Auth.hashPassword req.Password
                let cmd = new SqliteCommand("INSERT INTO Users (Username, Email, PasswordHash) VALUES (@u, @e, @h); SELECT last_insert_rowid();", conn)
                cmd.Parameters.AddWithValue("@u", req.Username) |> ignore
                cmd.Parameters.AddWithValue("@e", req.Email) |> ignore
                cmd.Parameters.AddWithValue("@h", hash) |> ignore
                let id = Convert.ToInt32(cmd.ExecuteScalar())
                
                // Initialize portfolio
                let cmd2 = new SqliteCommand("INSERT INTO Portfolios (UserId, CashBalance) VALUES (@id, 100000.0)", conn)
                cmd2.Parameters.AddWithValue("@id", id) |> ignore
                cmd2.ExecuteNonQuery() |> ignore
                
                return Ok id
            with e -> return Error e.Message
        }

    let getUserByEmail (email: string) : Async<(int * string * string * string) option> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("SELECT Id, Username, Email, PasswordHash FROM Users WHERE Email = @e", conn)
            cmd.Parameters.AddWithValue("@e", email) |> ignore
            use reader = cmd.ExecuteReader()
            if reader.Read() then
                return Some (reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3))
            else return None
        }

    let getPortfolio (userId: int) : Async<Portfolio option> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            
            // Get cash balance
            let cmd = new SqliteCommand("SELECT CashBalance FROM Portfolios WHERE UserId = @id", conn)
            cmd.Parameters.AddWithValue("@id", userId) |> ignore
            let cash = Convert.ToDouble(cmd.ExecuteScalar())
            
            // Get holdings
            let cmd2 = new SqliteCommand("SELECT Ticker, CompanyName, Shares, AverageBuyPrice FROM Holdings WHERE UserId = @id", conn)
            cmd2.Parameters.AddWithValue("@id", userId) |> ignore
            use reader = cmd2.ExecuteReader()
            let mutable holdings = []
            while reader.Read() do
                let ticker = reader.GetString(0)
                let name = reader.GetString(1)
                let shares = reader.GetDouble(2)
                let avgPrice = reader.GetDouble(3)
                
                let currentPrice = avgPrice 
                let currentValue = shares * currentPrice
                let profitLoss = currentValue - (shares * avgPrice)
                let profitLossPct = if avgPrice > 0.0 then (profitLoss / (shares * avgPrice)) * 100.0 else 0.0
                
                holdings <- {
                    Ticker = ticker
                    CompanyName = name
                    Shares = shares
                    AverageBuyPrice = avgPrice
                    CurrentPrice = currentPrice
                    CurrentValue = currentValue
                    ProfitLoss = profitLoss
                    ProfitLossPct = profitLossPct
                } :: holdings
            
            let totalInvested = holdings |> List.sumBy (fun h -> h.Shares * h.AverageBuyPrice)
            let totalCurrentValue = holdings |> List.sumBy (fun h -> h.CurrentValue)
            let totalValue = cash + totalCurrentValue
            let totalProfitLoss = totalCurrentValue - totalInvested
            let totalProfitLossPct = if totalInvested > 0.0 then (totalProfitLoss / totalInvested) * 100.0 else 0.0
            
            return Some {
                CashBalance = cash
                TotalInvested = totalInvested
                TotalCurrentValue = totalCurrentValue
                TotalValue = totalValue
                TotalProfitLoss = totalProfitLoss
                TotalProfitLossPct = totalProfitLossPct
                Holdings = holdings
            }
        }

    let executeTrade (userId: int) (req: TradeRequest) : Async<TradeResult> =
        async {
            try
                use conn = new SqliteConnection(connectionString)
                conn.Open()
                use trans = conn.BeginTransaction()
                
                let cmd = new SqliteCommand("SELECT CashBalance FROM Portfolios WHERE UserId = @id", conn, trans)
                cmd.Parameters.AddWithValue("@id", userId) |> ignore
                let cash = Convert.ToDouble(cmd.ExecuteScalar())
                
                let! quoteOpt = StockApi.getStockQuote req.Ticker
                match quoteOpt with
                | None -> return Failure "Could not fetch stock price"
                | Some quote ->
                    let totalCost = req.Shares * quote.Price
                    let mutable result = Success ""
                    
                    if req.TradeType = "BUY" then
                        if cash < totalCost then 
                            result <- Failure "Insufficient funds"
                        else
                            let cmd2 = new SqliteCommand("UPDATE Portfolios SET CashBalance = CashBalance - @cost WHERE UserId = @id", conn, trans)
                            cmd2.Parameters.AddWithValue("@cost", totalCost) |> ignore
                            cmd2.Parameters.AddWithValue("@id", userId) |> ignore
                            cmd2.ExecuteNonQuery() |> ignore
                            
                            let cmd3 = new SqliteCommand("SELECT Shares, AverageBuyPrice FROM Holdings WHERE UserId = @id AND Ticker = @t", conn, trans)
                            cmd3.Parameters.AddWithValue("@id", userId) |> ignore
                            cmd3.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                            use reader = cmd3.ExecuteReader()
                            if reader.Read() then
                                let oldShares = reader.GetDouble(0)
                                let oldAvg = reader.GetDouble(1)
                                let newShares = oldShares + req.Shares
                                let newAvg = ((oldShares * oldAvg) + totalCost) / newShares
                                reader.Close()
                                let cmd4 = new SqliteCommand("UPDATE Holdings SET Shares = @s, AverageBuyPrice = @a WHERE UserId = @id AND Ticker = @t", conn, trans)
                                cmd4.Parameters.AddWithValue("@s", newShares) |> ignore
                                cmd4.Parameters.AddWithValue("@a", newAvg) |> ignore
                                cmd4.Parameters.AddWithValue("@id", userId) |> ignore
                                cmd4.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                                cmd4.ExecuteNonQuery() |> ignore
                            else
                                reader.Close()
                                let cmd4 = new SqliteCommand("INSERT INTO Holdings (UserId, Ticker, CompanyName, Shares, AverageBuyPrice) VALUES (@id, @t, @n, @s, @a)", conn, trans)
                                cmd4.Parameters.AddWithValue("@id", userId) |> ignore
                                cmd4.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                                cmd4.Parameters.AddWithValue("@n", req.CompanyName) |> ignore
                                cmd4.Parameters.AddWithValue("@s", req.Shares) |> ignore
                                cmd4.Parameters.AddWithValue("@a", quote.Price) |> ignore
                                cmd4.ExecuteNonQuery() |> ignore
                            result <- Success (sprintf "Successfully bought %s shares of %s" (req.Shares.ToString("N2")) req.Ticker)
                    else
                        let cmd3 = new SqliteCommand("SELECT Shares FROM Holdings WHERE UserId = @id AND Ticker = @t", conn, trans)
                        cmd3.Parameters.AddWithValue("@id", userId) |> ignore
                        cmd3.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                        let obj = cmd3.ExecuteScalar()
                        let sharesOwned = if obj = null || obj = box DBNull.Value then 0.0 else Convert.ToDouble(obj)
                        if sharesOwned < req.Shares then 
                            result <- Failure "Insufficient shares"
                        else
                            let cmd2 = new SqliteCommand("UPDATE Portfolios SET CashBalance = CashBalance + @gain WHERE UserId = @id", conn, trans)
                            cmd2.Parameters.AddWithValue("@gain", totalCost) |> ignore
                            cmd2.Parameters.AddWithValue("@id", userId) |> ignore
                            cmd2.ExecuteNonQuery() |> ignore
                            
                            if sharesOwned = req.Shares then
                                let cmd4 = new SqliteCommand("DELETE FROM Holdings WHERE UserId = @id AND Ticker = @t", conn, trans)
                                cmd4.Parameters.AddWithValue("@id", userId) |> ignore
                                cmd4.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                                cmd4.ExecuteNonQuery() |> ignore
                            else
                                let cmd4 = new SqliteCommand("UPDATE Holdings SET Shares = Shares - @s WHERE UserId = @id AND Ticker = @t", conn, trans)
                                cmd4.Parameters.AddWithValue("@s", req.Shares) |> ignore
                                cmd4.Parameters.AddWithValue("@id", userId) |> ignore
                                cmd4.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                                cmd4.ExecuteNonQuery() |> ignore
                            result <- Success (sprintf "Successfully sold %s shares of %s" (req.Shares.ToString("N2")) req.Ticker)
                    
                    match result with
                    | Success _ ->
                        let cmdLog = new SqliteCommand("INSERT INTO Transactions (UserId, Ticker, CompanyName, TransactionType, Shares, PricePerShare, TotalValue, ExecutedAt) VALUES (@id, @t, @n, @type, @s, @p, @v, @date)", conn, trans)
                        cmdLog.Parameters.AddWithValue("@id", userId) |> ignore
                        cmdLog.Parameters.AddWithValue("@t", req.Ticker) |> ignore
                        cmdLog.Parameters.AddWithValue("@n", req.CompanyName) |> ignore
                        cmdLog.Parameters.AddWithValue("@type", req.TradeType) |> ignore
                        cmdLog.Parameters.AddWithValue("@s", req.Shares) |> ignore
                        cmdLog.Parameters.AddWithValue("@p", quote.Price) |> ignore
                        cmdLog.Parameters.AddWithValue("@v", totalCost) |> ignore
                        cmdLog.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o")) |> ignore
                        cmdLog.ExecuteNonQuery() |> ignore
                        trans.Commit()
                    | Failure _ -> trans.Rollback()
                    
                    return result
            with e -> return Failure e.Message
        }

    let getLeaderboard () : Async<LeaderboardEntry list> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("SELECT u.Username, p.CashBalance + IFNULL((SELECT SUM(h.Shares * h.AverageBuyPrice) FROM Holdings h WHERE h.UserId = u.Id), 0) as TotalValue FROM Users u JOIN Portfolios p ON u.Id = p.UserId ORDER BY TotalValue DESC LIMIT 10", conn)
            use reader = cmd.ExecuteReader()
            let mutable results = []
            let mutable rank = 1
            while reader.Read() do
                let totalValue = reader.GetDouble(1)
                results <- {
                    Rank = rank
                    Username = reader.GetString(0)
                    TotalValue = totalValue
                    ProfitLossPct = ((totalValue - 100000.0) / 100000.0) * 100.0
                } :: results
                rank <- rank + 1
            return List.rev results
        }

    let addToWatchlist (userId: int) (ticker: string) (companyName: string) : Async<unit> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("INSERT OR IGNORE INTO Watchlist (UserId, Ticker, CompanyName) VALUES (@id, @t, @n)", conn)
            cmd.Parameters.AddWithValue("@id", userId) |> ignore
            cmd.Parameters.AddWithValue("@t", ticker) |> ignore
            cmd.Parameters.AddWithValue("@n", companyName) |> ignore
            cmd.ExecuteNonQuery() |> ignore
        }

    let removeFromWatchlist (userId: int) (ticker: string) : Async<unit> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("DELETE FROM Watchlist WHERE UserId = @id AND Ticker = @t", conn)
            cmd.Parameters.AddWithValue("@id", userId) |> ignore
            cmd.Parameters.AddWithValue("@t", ticker) |> ignore
            cmd.ExecuteNonQuery() |> ignore
        }

    let getWatchlist (userId: int) : Async<WatchlistItem list> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("SELECT Ticker, CompanyName FROM Watchlist WHERE UserId = @id", conn)
            cmd.Parameters.AddWithValue("@id", userId) |> ignore
            use reader = cmd.ExecuteReader()
            let mutable results = []
            while reader.Read() do
                results <- { Ticker = reader.GetString(0); CompanyName = reader.GetString(1) } :: results
            return List.rev results
        }

    let getTransactions (userId: int) : Async<Transaction list> =
        async {
            use conn = new SqliteConnection(connectionString)
            conn.Open()
            let cmd = new SqliteCommand("SELECT Id, Ticker, CompanyName, TransactionType, Shares, PricePerShare, TotalValue, ExecutedAt FROM Transactions WHERE UserId = @id ORDER BY ExecutedAt DESC", conn)
            cmd.Parameters.AddWithValue("@id", userId) |> ignore
            use reader = cmd.ExecuteReader()
            let mutable results = []
            while reader.Read() do
                results <- {
                    Id = reader.GetInt32(0)
                    Ticker = reader.GetString(1)
                    CompanyName = reader.GetString(2)
                    TransactionType = reader.GetString(3)
                    Shares = reader.GetDouble(4)
                    PricePerShare = reader.GetDouble(5)
                    TotalValue = reader.GetDouble(6)
                    ExecutedAt = reader.GetString(7)
                } :: results
            return List.rev results
        }

// working on client side portfolio view improvements...