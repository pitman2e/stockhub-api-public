# Domain glossary

## Portfolio
A real investment portfolio: a concrete collection of holdings owned by an investor. A portfolio belongs to one user and has a default currency used when converting position values for reporting.

## Virtual portfolio
A logical reporting container stored as a `stock_portfolio` row whose `is_virtual` value is true. It is not a separate investment account or holding. Its membership is stored in `stock_virtual_portfolio`: `portfolio_id` is the virtual parent and `child_portfolio_id` is a real portfolio owned by the same user.

Virtual portfolios can contain only real portfolios. A real portfolio cannot contain children, and virtual portfolios cannot be nested or contain themselves. The API validates that every proposed child ID exists for the authenticated user and is non-virtual. Portfolio Add/Edit submits the complete proposed `childPortfolioIds` list; the server reconciles the mapping rows only when the user confirms the form, so removing a chip is only a draft change until submission succeeds.

Portfolio queries expand a virtual portfolio to its mapped real portfolios through `ByPortfolioId_All`. Position calculations therefore continue to use the existing portfolio endpoints while reporting across the selected members. After a virtual portfolio is added or edited, the API refreshes position snapshots through `UpdateStockPositionAsync` so the persisted calculation reflects the current mapping. Removing a child removes its membership, not the child's own real portfolio or its position history.

## Position
A holding in a portfolio for a specific security, identified by user, portfolio, and stock ID. Positions are calculated over time and include quantity, cost, realised and unrealised gains, dividends, and current-day performance metrics.

## Performance snapshot
A point-in-time record of a portfolio's performance for a specific day. It captures the position state as of an observation date, while market values may still come from the latest available trading day.

## Market date vs. observe date
The system separates the trading-day market date from the logical business date. `MarketDate` is the last available market price date for the stock, while `ObserveDate` is the reporting date being evaluated. This allows weekend or non-trading days to still show the most recent available valuation.

## Stock
A security identified by its ticker and exchange. The same company listed on different exchanges is treated as a different stock. Stocks can also belong to an asset class and carry their own pricing and dividend history.

## Bond
A fixed-income instrument that is tracked like a security, but with additional bond-specific conventions such as accrued interest and cost basis treatment. Bond positions may still be valued using market price or transaction fallback rules.

## Transaction
An investment event affecting a position. The core transaction types are:
- BUY: increases quantity and cost basis.
- SELL: reduces quantity, creates realised proceeds, and can affect current gain on the sale day.
- DIV: cash dividend; increases realised dividend and income.
- CASH: cash movement that is treated as realised cash income rather than a security purchase.
- REINV: dividend paid as stock and immediately reinvested; treated as both dividend income and a buy event.

## Realised stock
A stock position that has been sold, so its gain or loss is considered realised. The realised portion is separated from the remaining unrealised value of still-held shares.

## Realised scrip event
A dividend event in which the broker pays stock and immediately reinvests it, with the quantity and reinvestment price recorded. In the code, this is modelled as `REINV`, which behaves like dividend income plus a buy transaction.

## Dividend and ex-date
Dividend events carry announce, ex-dividend, and payable dates. The ex-date is treated as the point at which the share price is adjusted for the dividend. Current gain calculations exclude the price drop caused by ex-dividend effects when the ex-date falls on the valuation day.

## Unrealised and realised values
The position model tracks both unrealised and realised performance:
- Unrealised amount = quantity × market price.
- Unrealised cost = remaining cost basis still held.
- Unrealised gain = unrealised amount − unrealised cost.
- Realised amount = proceeds already locked in from sales and cash distributions.
- Realised gain = realised amount − realised cost.
- Realised dividend = dividend cash received, including reinvestment cash portions.

## Current gain
Current gain is the day-to-day price movement component for the open position, excluding the dividend-related price drop on ex-dividend dates and adjusted for sells on the same day. It is used for short-term performance analysis and is separate from realised gain.

## Watchlist
A saved list of symbols for monitoring. It is a user-facing collection rather than a portfolio or position record. A saved symbol remains listed when market data is unavailable; its price and movement values are unavailable rather than zero.

## Tag
A user-defined classification used to group and visualize holdings, with no business-rule semantics. Tags are labels rather than accounting logic.

## Price source
The current market price is the preferred source for valuation. Transaction history is used only as a fallback when current market price is unavailable. The valuation engine also prefers the latest market price available for the period under analysis while preserving transaction-based prices where needed for exact position math.

## Default currency and conversion
Positions are evaluated in the portfolio's default currency, with exchange rates applied when transaction or stock values are stored in a different currency. Reporting values therefore reconcile both securities pricing and portfolio currency conversion.
