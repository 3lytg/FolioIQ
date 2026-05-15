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
| Database | SQLite |
| Styling | Tailwind CSS |
| Stock Data | Alpha Vantage API |

## Build & Run Locally

**Prerequisites:** .NET 8 SDK

1. **Clone the repository:**
   ```bash
   git clone https://github.com/3lytg/FolioIQ.git
   cd FolioIQ
   ```

2. **Configure API Key:**
   Open `appsettings.json` and add your [Alpha Vantage API Key](https://www.alphavantage.co/support/#api-key):
   ```json
   "AlphaVantage": {
     "ApiKey": "YOUR_KEY_HERE"
   }
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```
   *Note: The SQLite database (`folioiq.db`) will be automatically initialized on the first run.*

4. **Access the app:**
   Open [http://localhost:5000](http://localhost:5000) in your browser.

## License

MIT
