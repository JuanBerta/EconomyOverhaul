---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 89238829fb97168b9c4195c03c152aef_2b28f5c1a4f211f1903b525400f8a581
    ReservedCode1: BiYxlfUJbYtwS6HPO+4JlM2cinllnW7BQ3m7/oXFRYlnSDJxipFEdki3xGzcXmm01YtYP7VNBdNulEEMSK6vM3v16z0uyLjjj5zH9N8optQ1C9y+W9f1ef/bOsW5ExEu0uEa4wqj7sO1bTaBZAf8sxkB/h5zZvVCIdbhboSBGJpRnaTvXHzE7Zm9dX4=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 89238829fb97168b9c4195c03c152aef_2b28f5c1a4f211f1903b525400f8a581
    ReservedCode2: BiYxlfUJbYtwS6HPO+4JlM2cinllnW7BQ3m7/oXFRYlnSDJxipFEdki3xGzcXmm01YtYP7VNBdNulEEMSK6vM3v16z0uyLjjj5zH9N8optQ1C9y+W9f1ef/bOsW5ExEu0uEa4wqj7sO1bTaBZAf8sxkB/h5zZvVCIdbhboSBGJpRnaTvXHzE7Zm9dX4=
---

# EconomyOverhaul - Phase 2 Design: Investment System (Stocks & Real Estate)

Status: Implemented (v0.25.0)
Author: Beinded / Marvis

## 1. Goals

- Add a fictional exchange market: stocks (guilds/companies) and real estate
  (properties) with daily price variability and long-term inflation.
- Buying/selling uses the bank balance (`container_deposit`), so the investment
  account is the bank itself.
- Held assets generate daily income (dividends/rent) and expenses
  (maintenance/taxes); net profit is credited to (or debited from) the bank.
- Investment state (market factors, holdings, elapsed days) persists across
  game sessions.
- Reuse the existing IMGUI window: add a Stock and Real Estate tab.

Out of scope (Phase 3+): business management, manager AI, city-wide economy,
caravans, migration.

## 2. Market Model

### 2.1 Assets

Static definitions in `Investing/AssetDef.cs`. Each asset:

| Field | Meaning |
|---|---|
| Id | stable key, persisted |
| Name | display name |
| Type | Stock / RealEstate |
| BasePrice | price at day 0, no inflation |
| Volatility | per-day random-walk sigma (±) |
| IncomeRate | daily income as fraction of current price |
| ExpenseRate | daily expense as fraction of current price |

Initial assets (tunable in code):

| Id | Name | Type | Base | Vol | Income | Expense |
|---|---|---|---|---|---|---|
| stock_blacksmith | Blacksmith Guild | Stock | 500 | 4% | 0.8% | 0.1% |
| stock_farm | Harvest Farm Co. | Stock | 300 | 3% | 0.6% | 0.2% |
| stock_tavern | Ale Tavern Inc. | Stock | 400 | 5% | 1.0% | 0.2% |
| stock_adventurer | Adventurer Hall | Stock | 600 | 6% | 0.7% | 0.1% |
| stock_weaver | Weaver Mill | Stock | 350 | 3.5% | 0.5% | 0.1% |
| re_cottage | Village Cottage | RealEstate | 2000 | 2% | 0.4% | 0.2% |
| re_farm | Suburban Farm | RealEstate | 3500 | 2.5% | 0.6% | 0.3% |
| re_shop | Town Shop Front | RealEstate | 5000 | 3% | 0.8% | 0.4% |
| re_manor | Noble Manor | RealEstate | 12000 | 4% | 1.0% | 0.6% |

### 2.2 Price formula

    price(asset, day) = BasePrice * InflationFactor(day) * Factor(asset)

- `InflationFactor(day) = (1 + MarketDailyInflation)^day` (default 0.05%/day,
  ~20% per game year).
- `Factor(asset)` is a mean-reverting random walk, re-rolled every day:

    f = 1 + (f - 1) * 0.85 + Uniform(-Volatility, +Volatility)
    f = Clamp(f, 0.25, 3.5)

Mean reversion keeps prices near fundamentals while still trending.

### 2.3 Daily tick (hooked to `GameDate.AdvanceDay`)

1. Advance global day counter.
2. Re-roll every asset factor.
3. Settle holdings: for each held asset,
   `income - expense = shares * price * (IncomeRate - ExpenseRate)`, applied to
   the bank balance. Debts are clamped so the bank balance never goes negative.

## 3. Persistence

- File: `<save>/economyoverhaul_invest.json` (`GameIO.pathCurrentSave`).
- Model `InvestmentSaveData`: DayCount, Factors, Holdings. Plain
  `[JsonProperty]`-friendly POCO (Newtonsoft), saved with `GameIO.SaveFile`
  / `GameIO.LoadFile<T>` using the game's own JSON settings.
- Hooks:
  - `GameIO.SaveGame` Postfix -> save market state.
  - `GameIO.LoadGame` Postfix -> load market state.
  - `Game.StartNewGame` Postfix -> reset to fresh market (no stale state when
    starting a new run in the same process).
  - Trades also write to disk immediately (`SaveNow`), so a crash right after
    a purchase does not lose the transaction.

## 4. UI (IMGUI, same window)

- Window grows to 520x420 and gets three tabs: Bank / Stocks / Real Estate
  (`GUILayout.Toolbar`).
- Asset tabs: per asset one row: name, current price, holdings, per-day
  income/expense preview, Buy/Sell buttons with a shared quantity field.
- Market info line: day number + current inflation multiplier.

## 5. New Config Entries (BepInEx cfg)

| Section | Key | Default | Meaning |
|---|---|---|---|
| Market | MarketDailyInflation | 0.0005 | daily inflation (0.05%) |
| Market | MarketVolatility | 0.03 | default per-day price noise (unused per-asset now) |
| Market | InvestNotifyDaily | true | log daily settlement message |

(Volatility is currently defined per asset; the cfg entry is reserved for a
global multiplier if we later want a single tuning knob.)

## 6. Risk & Notes

- Prices are simulated, not tied to the real game economy; acceptable by design.
- `container_deposit` can go negative if a modded feature pays out; we clamp
  settlement debits at 0 balance.
- Cloud saves: the JSON file lives inside the save dir, so Steam Cloud zip
  packaging picks it up.
- If `GameIO.LoadFile<T>` deserialization fails (schema change), the mod logs
  the error and falls back to a fresh market instead of crashing.

## 7. Test Plan

1. Fresh save: open F8 -> Stocks tab -> buy 2 Blacksmith Guild shares; balance
   drops by 2 * price; holdings show 2.
2. Advance a day: prices change; settlement message shows net income (or
   expense); bank balance changes accordingly.
3. Save & reload: prices, day count, holdings survive.
4. New game (same process): market resets to day 0, default prices, no holdings.
5. Buy more than balance: rejected with message, no state change.
*（内容由AI生成，仅供参考）*
