namespace FolioIQ

open WebSharper

module Server =

    [<Remote>]
    let Register (req: RegisterRequest) : Async<Result<unit, string>> =
        async {
            let! res = Database.registerUser req
            match res with
            | Ok _ -> return Ok ()
            | Error e -> return Error e
        }

    [<Remote>]
    let Login (req: LoginRequest) : Async<Result<UserSession, string>> =
        async {
            let! userOpt = Database.getUserByEmail req.Email
            match userOpt with
            | Some (id, username, email, hash) ->
                if Auth.verifyPassword req.Password hash then
                    do! Auth.loginUser id
                    return Ok { UserId = id; Username = username; Email = email }
                else
                    return Error "Invalid email or password"
            | None ->
                return Error "Invalid email or password"
        }

    [<Remote>]
    let Logout () : Async<unit> =
        Auth.logoutUser ()

    [<Remote>]
    let GetPortfolio () : Async<Portfolio option> =
        async {
            let! userIdOpt = Auth.getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return! Database.getPortfolio userId
            | None -> return None
        }

    [<Remote>]
    let SearchStocks (query: string) : Async<StockSearchResult list> =
        StockApi.searchStocks query

    [<Remote>]
    let GetStockQuote (ticker: string) : Async<StockQuote option> =
        StockApi.getStockQuote ticker

    [<Remote>]
    let ExecuteTrade (req: TradeRequest) : Async<TradeResult> =
        async {
            let! userIdOpt = Auth.getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return! Database.executeTrade userId req
            | None -> return Failure "Not authenticated"
        }

    [<Remote>]
    let GetLeaderboard () : Async<LeaderboardEntry list> =
        Database.getLeaderboard()

    [<Remote>]
    let AddToWatchlist (ticker: string) (companyName: string) : Async<unit> =
        async {
            let! userIdOpt = Auth.getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return! Database.addToWatchlist userId ticker companyName
            | None -> ()
        }

    [<Remote>]
    let RemoveFromWatchlist (ticker: string) : Async<unit> =
        async {
            let! userIdOpt = Auth.getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return! Database.removeFromWatchlist userId ticker
            | None -> ()
        }

    [<Remote>]
    let GetWatchlist () : Async<WatchlistItem list> =
        async {
            let! userIdOpt = Auth.getLoggedInUserId()
            match userIdOpt with
            | Some userId -> return! Database.getWatchlist userId
            | None -> return []
        }

// working on update readme to reflect sqlite and actual setup steps...