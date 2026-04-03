namespace FolioIQ

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/dashboard">] Dashboard
    | [<EndPoint "/stock">] Stock of ticker: string
    | [<EndPoint "/leaderboard">] Leaderboard

module Site =

    let HomePage (ctx: Context<EndPoint>) : Async<Content<EndPoint>> =
        let doc : Doc = 
            Templates.Main()
                .MainContent(Doc.ClientSide <@ Client.AuthForm() @>)
                .Doc()
        Content.Page(doc)

    let DashboardPage (ctx: Context<EndPoint>) : Async<Content<EndPoint>> =
        let doc : Doc = 
            Templates.Dashboard()
                .MainContent(Doc.ClientSide <@ Client.Dashboard() @>)
                .Doc()
        Content.Page(doc)

    let StockPage (ctx: Context<EndPoint>) ticker : Async<Content<EndPoint>> =
        let doc : Doc = 
            Templates.Stock()
                .MainContent(Doc.ClientSide <@ Client.StockDetail ticker @>)
                .Doc()
        Content.Page(doc)

    let LeaderboardPage (ctx: Context<EndPoint>) : Async<Content<EndPoint>> =
        let doc : Doc = 
            Templates.Leaderboard()
                .MainContent(Doc.ClientSide <@ Client.Leaderboard() @>)
                .Doc()
        Content.Page(doc)

    [<Website>]
    let Main =
        Application.MultiPage (fun (ctx: Context<EndPoint>) (endpoint: EndPoint) ->
            match endpoint with
            | Home -> HomePage ctx
            | Dashboard -> DashboardPage ctx
            | Stock ticker -> StockPage ctx ticker
            | Leaderboard -> LeaderboardPage ctx
        )

// working on fix cash balance update on sell, was adding wrong amount...