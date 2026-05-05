namespace FolioIQ

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Templating

[<JavaScript>]
module Templates =
    type Main = Template<"wwwroot/index.html", ClientLoad.FromDocument>
    type Dashboard = Template<"wwwroot/dashboard.html", ClientLoad.FromDocument>
    type Stock = Template<"wwwroot/stock.html", ClientLoad.FromDocument>
    type Leaderboard = Template<"wwwroot/leaderboard.html", ClientLoad.FromDocument>

[<JavaScript>]
module Client =

    let formatCurrency (v: float) = sprintf "€%s" (v.ToString("N2"))
    let formatPct (v: float) = 
        let sign = if v >= 0.0 then "+" else ""
        sprintf "%s%s%%" sign (v.ToString("N2"))
    let pctColor (v: float) = if v >= 0.0 then "text-emerald-600" else "text-rose-600"

    let AuthForm () =
        let isLogin = Var.Create true
        let email = Var.Create ""
        let username = Var.Create ""
        let password = Var.Create ""
        let error = Var.Create ""

        let submit () =
            async {
                error.Set ""
                if isLogin.Value then
                    let! res = Server.Login { Email = email.Value; Password = password.Value }
                    match res with
                    | Ok _ -> JS.Window.Location.Replace("/dashboard")
                    | Error e -> error.Set e
                else
                    let! res = Server.Register { Email = email.Value; Username = username.Value; Password = password.Value }
                    match res with
                    | Ok _ -> 
                        let! loginRes = Server.Login { Email = email.Value; Password = password.Value }
                        JS.Window.Location.Replace("/dashboard")
                    | Error e -> error.Set e
            } |> Async.Start

        div [] [
            h2 [attr.``class`` "text-2xl font-bold mb-6"] [text (if isLogin.Value then "Welcome Back" else "Create Account")]
            
            div [attr.``class`` "space-y-4"] [
                div [] [
                    label [attr.``class`` "block text-sm font-medium text-slate-700 mb-1"] [text "Email"]
                    Doc.Input [attr.``class`` "w-full px-4 py-2 border border-slate-200 rounded-lg outline-none focus:ring-2 focus:ring-indigo-500"] email
                ]
                (if not isLogin.Value then
                    div [] [
                        label [attr.``class`` "block text-sm font-medium text-slate-700 mb-1"] [text "Username"]
                        Doc.Input [attr.``class`` "w-full px-4 py-2 border border-slate-200 rounded-lg outline-none focus:ring-2 focus:ring-indigo-500"] username
                    ]
                 else Doc.Empty)
                div [] [
                    label [attr.``class`` "block text-sm font-medium text-slate-700 mb-1"] [text "Password"]
                    Doc.PasswordBox [attr.``class`` "w-full px-4 py-2 border border-slate-200 rounded-lg outline-none focus:ring-2 focus:ring-indigo-500"] password
                ]
                
                Doc.EmbedView (error.View |> View.Map (fun e ->
                    if e = "" then Doc.Empty
                    else div [attr.``class`` "p-3 bg-rose-50 text-rose-600 text-sm rounded-lg"] [text e]
                ))

                button [
                    attr.``class`` "w-full py-3 bg-indigo-600 text-white font-bold rounded-lg hover:bg-indigo-700 transition-colors"
                    on.click (fun _ _ -> submit())
                ] [text (if isLogin.Value then "Sign In" else "Get Started")]

                div [attr.``class`` "text-center mt-4"] [
                    button [
                        attr.``class`` "text-sm text-indigo-600 hover:underline"
                        on.click (fun _ _ -> isLogin.Set (not isLogin.Value))
                    ] [text (if isLogin.Value then "Need an account? Register" else "Already have an account? Login")]
                ]
            ]
        ]

    let Dashboard () =
        let portfolio = Var.Create None
        let refresh () =
            async {
                let! p = Server.GetPortfolio()
                portfolio.Set p
            } |> Async.Start

        refresh()

        Templates.Dashboard()
            .MainContent(Doc.EmbedView (portfolio.View |> View.Map (function
                | Some p -> 
                    div [] [
                        div [attr.``class`` "grid grid-cols-1 md:grid-cols-4 gap-6 mb-8"] [
                            div [attr.``class`` "bg-white p-6 rounded-2xl border border-slate-100 shadow-sm"] [
                                div [attr.``class`` "text-sm text-slate-500 mb-1"] [text "Total Value"]
                                div [attr.``class`` "text-2xl font-bold text-slate-900"] [text (formatCurrency p.TotalValue)]
                                div [attr.``class`` (pctColor p.TotalProfitLoss + " text-sm mt-1")] [text (sprintf "%s (%s)" (formatCurrency p.TotalProfitLoss) (formatPct p.TotalProfitLossPct))]
                            ]
                            div [attr.``class`` "bg-white p-6 rounded-2xl border border-slate-100 shadow-sm"] [
                                div [attr.``class`` "text-sm text-slate-500 mb-1"] [text "Cash Balance"]
                                div [attr.``class`` "text-2xl font-bold text-slate-900"] [text (formatCurrency p.CashBalance)]
                            ]
                            div [attr.``class`` "bg-white p-6 rounded-2xl border border-slate-100 shadow-sm"] [
                                div [attr.``class`` "text-sm text-slate-500 mb-1"] [text "Total Invested"]
                                div [attr.``class`` "text-2xl font-bold text-slate-900"] [text (formatCurrency p.TotalInvested)]
                            ]
                        ]
                        
                        table [attr.``class`` "w-full"] [
                            thead [attr.``class`` "bg-slate-50 border-y border-slate-100"] [
                                tr [] [
                                    th [attr.``class`` "px-6 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Asset"]
                                    th [attr.``class`` "px-6 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Shares"]
                                    th [attr.``class`` "px-6 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Avg Price"]
                                    th [attr.``class`` "px-6 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Market Price"]
                                    th [attr.``class`` "px-6 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Market Value"]
                                    th [attr.``class`` "px-6 py-4 text-right text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Profit/Loss"]
                                ]
                            ]
                            tbody [] (
                                p.Holdings |> List.map (fun h ->
                                    tr [attr.``class`` "hover:bg-slate-50 cursor-pointer"; on.click (fun _ _ -> JS.Window.Location.Assign("/stock/" + h.Ticker))] [
                                        td [attr.``class`` "px-6 py-4"] [
                                            div [attr.``class`` "font-bold text-slate-900"] [text h.Ticker]
                                            div [attr.``class`` "text-xs text-slate-500"] [text h.CompanyName]
                                        ]
                                        td [attr.``class`` "px-6 py-4 text-slate-600"] [text (h.Shares.ToString("N2"))]
                                        td [attr.``class`` "px-6 py-4 text-slate-600"] [text (formatCurrency h.AverageBuyPrice)]
                                        td [attr.``class`` "px-6 py-4 text-slate-600"] [text (formatCurrency h.CurrentPrice)]
                                        td [attr.``class`` "px-6 py-4 font-semibold text-slate-900"] [text (formatCurrency h.CurrentValue)]
                                        td [attr.``class`` "px-6 py-4 text-right"] [
                                            div [attr.``class`` (pctColor h.ProfitLoss)] [text (formatCurrency h.ProfitLoss)]
                                            div [attr.``class`` ("text-xs " + pctColor h.ProfitLoss)] [text (formatPct h.ProfitLossPct)]
                                        ]
                                    ] :> Doc
                                )
                            )
                        ]
                    ]
                | None -> text "Loading..."
            )))
            .Doc()

    let StockDetail (ticker: string) =
        let quote = Var.Create None
        let tradeType = Var.Create "BUY"
        let sharesStr = Var.Create "0"
        let message = Var.Create ""

        let refresh () =
            async {
                let! q = Server.GetStockQuote ticker
                quote.Set q
            } |> Async.Start

        let executeTrade (q: StockQuote) =
            async {
                let s = match System.Double.TryParse(sharesStr.Value) with (true, v) -> v | _ -> 0.0
                let req = { Ticker = ticker; CompanyName = q.CompanyName; Shares = s; TradeType = tradeType.Value }
                let! res = Server.ExecuteTrade req
                match res with
                | Success m -> message.Set m; refresh()
                | Failure e -> message.Set e
            } |> Async.Start

        refresh()

        Templates.Stock()
            .MainContent(Doc.EmbedView (quote.View |> View.Map (function
                | Some q -> 
                    div [] [
                        div [attr.``class`` "mb-8"] [
                            h1 [attr.``class`` "text-4xl font-bold text-slate-900"] [text ticker]
                            p [attr.``class`` "text-slate-500"] [text q.CompanyName]
                        ]
                        div [attr.``class`` "flex items-baseline space-x-4 mb-8"] [
                            span [attr.``class`` "text-5xl font-bold text-slate-900"] [text (formatCurrency q.Price)]
                            span [attr.``class`` (pctColor q.Change + " text-xl font-semibold")] [text (sprintf "%s (%s)" (formatCurrency q.Change) (formatPct q.ChangePct))]
                        ]
                        div [attr.``class`` "bg-white p-8 rounded-2xl border border-slate-100 shadow-sm"] [
                            h3 [attr.``class`` "text-lg font-bold mb-6"] [text "Execute Trade"]
                            div [attr.``class`` "space-y-6"] [
                                div [attr.``class`` "flex p-1 bg-slate-100 rounded-xl"] [
                                    button [
                                        attr.``class`` ("flex-1 py-2 rounded-lg font-bold transition-all " + if tradeType.Value = "BUY" then "bg-white text-indigo-600 shadow-sm" else "text-slate-500 hover:text-slate-700")
                                        on.click (fun _ _ -> tradeType.Set "BUY")
                                    ] [text "Buy"]
                                    button [
                                        attr.``class`` ("flex-1 py-2 rounded-lg font-bold transition-all " + if tradeType.Value = "SELL" then "bg-white text-rose-600 shadow-sm" else "text-slate-500 hover:text-slate-700")
                                        on.click (fun _ _ -> tradeType.Set "SELL")
                                    ] [text "Sell"]
                                ]
                                div [] [
                                    label [attr.``class`` "block text-sm font-medium text-slate-700 mb-1"] [text "Shares"]
                                    Doc.Input [attr.``class`` "w-full px-4 py-3 border border-slate-200 rounded-xl outline-none focus:ring-2 focus:ring-indigo-500"] sharesStr
                                ]
                                Doc.EmbedView (message.View |> View.Map (fun m ->
                                    if m = "" then Doc.Empty
                                    else div [attr.``class`` "p-4 bg-indigo-50 text-indigo-600 rounded-xl text-sm font-medium"] [text m]
                                ))
                                button [
                                    attr.``class`` ("w-full py-4 text-white font-bold rounded-xl transition-colors " + if tradeType.Value = "BUY" then "bg-indigo-600 hover:bg-indigo-700" else "bg-rose-600 hover:bg-rose-700")
                                    on.click (fun _ _ -> executeTrade q)
                                ] [text (if tradeType.Value = "BUY" then "Confirm Purchase" else "Confirm Sale")]
                            ]
                        ]
                    ]
                | None -> text "Loading..."
            )))
            .Doc()

    let Leaderboard () =
        let entries = Var.Create []
        
        async {
            let! res = Server.GetLeaderboard()
            entries.Set res
        } |> Async.Start

        Templates.Leaderboard()
            .MainContent(Doc.EmbedView (entries.View |> View.Map (fun items ->
                table [attr.``class`` "w-full"] [
                    thead [attr.``class`` "bg-slate-50 border-y border-slate-100"] [
                        tr [] [
                            th [attr.``class`` "px-8 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Rank"]
                            th [attr.``class`` "px-8 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Trader"]
                            th [attr.``class`` "px-8 py-4 text-left text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Total Value"]
                            th [attr.``class`` "px-8 py-4 text-right text-xs font-bold text-slate-500 uppercase tracking-wider"] [text "Profit/Loss"]
                        ]
                    ]
                    tbody [] (
                        items |> List.map (fun e ->
                            tr [attr.``class`` "hover:bg-slate-50"] [
                                td [attr.``class`` "px-8 py-4 font-bold text-slate-400"] [text (sprintf "#%d" e.Rank)]
                                td [attr.``class`` "px-8 py-4 font-bold text-slate-900"] [text e.Username]
                                td [attr.``class`` "px-8 py-4 text-slate-600"] [text (formatCurrency e.TotalValue)]
                                td [attr.``class`` "px-8 py-4 text-right"] [
                                    span [attr.``class`` (pctColor e.ProfitLossPct + " font-bold")] [text (formatPct e.ProfitLossPct)]
                                ]
                            ] :> Doc
                        )
                    )
                ]
            )))
            .Doc()

// working on fix double dispose on httpclient in stock api...