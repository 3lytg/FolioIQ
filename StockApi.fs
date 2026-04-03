namespace FolioIQ

open System
open System.Net.Http
open System.Collections.Concurrent
open System.Text.Json

module StockApi =

    let private cache = ConcurrentDictionary<string, DateTime * obj>()
    let private cacheDuration = TimeSpan.FromMinutes(5.0)

    let private getApiKey () = 
        "DEMO_KEY"

    let private fetchWithCache<'T> (key: string) (fetcher: unit -> Async<'T>) : Async<'T> =
        async {
            match cache.TryGetValue(key) with
            | true, (timestamp, data) when DateTime.UtcNow - timestamp < cacheDuration ->
                return data :?> 'T
            | _ ->
                let! data = fetcher()
                cache.[key] <- (DateTime.UtcNow, data :> obj)
                return data
        }

    let searchStocks (query: string) : Async<StockSearchResult list> =
        let apiKey = getApiKey()
        let url = sprintf "https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords=%s&apikey=%s" query apiKey
        fetchWithCache (sprintf "search_%s" query) (fun () ->
            async {
                try
                    use client = new HttpClient()
                    let! response = client.GetStringAsync(url) |> Async.AwaitTask
                    let doc = JsonDocument.Parse(response)
                    let matches = doc.RootElement.GetProperty("bestMatches")
                    return [
                        for m in matches.EnumerateArray() do
                            yield {
                                Ticker = m.GetProperty("1. symbol").GetString()
                                CompanyName = m.GetProperty("2. name").GetString()
                                Region = m.GetProperty("4. region").GetString()
                                Currency = m.GetProperty("8. currency").GetString()
                            }
                    ]
                with _ -> return []
            }
        )

    let getStockQuote (ticker: string) : Async<StockQuote option> =
        let apiKey = getApiKey()
        let url = sprintf "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=%s&apikey=%s" ticker apiKey
        fetchWithCache (sprintf "quote_%s" ticker) (fun () ->
            async {
                try
                    use client = new HttpClient()
                    let! response = client.GetStringAsync(url) |> Async.AwaitTask
                    let doc = JsonDocument.Parse(response)
                    let quote = doc.RootElement.GetProperty("Global Quote")
                    if quote.EnumerateObject() |> Seq.isEmpty then return None
                    else
                        let priceStr = quote.GetProperty("05. price").GetString()
                        let changeStr = quote.GetProperty("09. change").GetString()
                        let changePctStr = quote.GetProperty("10. change percent").GetString().Replace("%", "")
                        let volumeStr = quote.GetProperty("06. volume").GetString()
                        
                        return Some {
                            Ticker = quote.GetProperty("01. symbol").GetString()
                            CompanyName = "" 
                            Price = Double.Parse(priceStr)
                            Change = Double.Parse(changeStr)
                            ChangePct = Double.Parse(changePctStr)
                            Volume = Int64.Parse(volumeStr)
                        }
                with _ -> return None
            }
        )

    let getPriceHistory (ticker: string) : Async<(string * float) list> =
        let apiKey = getApiKey()
        let url = sprintf "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol=%s&outputsize=compact&apikey=%s" ticker apiKey
        fetchWithCache (sprintf "history_%s" ticker) (fun () ->
            async {
                try
                    use client = new HttpClient()
                    let! response = client.GetStringAsync(url) |> Async.AwaitTask
                    let doc = JsonDocument.Parse(response)
                    let series = doc.RootElement.GetProperty("Time Series (Daily)")
                    let results = 
                        series.EnumerateObject()
                        |> Seq.take 7
                        |> Seq.map (fun prop -> 
                            let date = prop.Name
                            let close = prop.Value.GetProperty("4. close").GetString() |> Double.Parse
                            (date, close)
                        )
                        |> Seq.toList
                        |> List.rev
                    return results
                with _ -> return []
            }
        )

// working on fix cash balance update on sell, was adding wrong amount...