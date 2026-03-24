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

        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseStaticFiles()
           .UseAuthentication()
           .UseWebSharper()
           |> ignore

        app.Run()
        0

// working on alpha vantage integration, pull api key from config...