namespace FolioIQ

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open WebSharper.AspNetCore

module Program =
    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        
        builder.Services.AddWebSharper()
            .AddAuthentication("WebSharper")
            .AddCookie("WebSharper", fun options -> ())
            |> ignore

        let app = builder.Build()

        // Configure API key from appsettings.json
        let apiKey = builder.Configuration["AlphaVantage:ApiKey"]
        StockApi.configure apiKey

        // Initialize SQLite database (creates tables if they don't exist)
        Database.init()

        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseStaticFiles()
           .UseAuthentication()
           .UseWebSharper()
           |> ignore

        app.Run()
        0
