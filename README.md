# FolioIQ — Paper Trading & Portfolio Simulator

> Learn to invest without losing real money.

FolioIQ is a full-stack F# web application that gives university students €10,000 
in virtual money to invest in real stocks at live market prices. Track your portfolio, 
analyze performance, and compete on the global leaderboard.

## Motivation

Most young people want to start investing but fear losing money while learning the basics. 
FolioIQ removes that barrier — practice with real market data, zero financial risk.

## Features

- 📈 Real-time stock prices via Alpha Vantage API
- 💼 Virtual portfolio starting with €10,000
- 🔄 Buy and sell stocks at live market prices
- 📊 Portfolio performance dashboard with P&L tracking
- 🏆 Global leaderboard — compete with other users
- 📋 Full transaction history
- ⭐ Watchlist for tracking stocks before buying
- 📉 7-day price chart for every stock

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | F# / .NET 8 |
| Framework | WebSharper (client-server) |
| Database | SQL Server + SqlHydra |
| Styling | Tailwind CSS |
| Stock Data | Alpha Vantage API |
| Hosting | Azure App Service |
| CI/CD | GitHub Actions |

## Build & Run Locally

**Prerequisites:** .NET 8 SDK, SQL Server (or LocalDB)

```bash
git clone https://github.com/YOUR_USERNAME/folioiq.git
cd folioiq

# Set up database
sqlcmd -S "(localdb)\MSSQLLocalDB" -i schema.sql

# Add your Alpha Vantage API key to appsettings.json

dotnet run
```

Open http://localhost:5000

## License

MIT
