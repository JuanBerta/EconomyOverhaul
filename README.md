---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 89238829fb97168b9c4195c03c152aef_32ed466bce5c11f1aa22525400393706
    ReservedCode1: /gyq0tu0+dLWix0DGyOb7DO9Y7wL/s3KSIsPS8iCPC5OepG7DV8Razc1YTmy2VEl+ZBrjNIe4GeZ2qZ4B9LxUXWHy3s4QbYWsjDy886joTD6aeVIwtoX/FTxzrerTr6n7VaaIBku50aFJOB6E7Cw2ZFOXgkBUdgFZVafRlJ2cdHB0HIlJvk3vEMZD94=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 89238829fb97168b9c4195c03c152aef_32ed466bce5c11f1aa22525400393706
    ReservedCode2: /gyq0tu0+dLWix0DGyOb7DO9Y7wL/s3KSIsPS8iCPC5OepG7DV8Razc1YTmy2VEl+ZBrjNIe4GeZ2qZ4B9LxUXWHy3s4QbYWsjDy886joTD6aeVIwtoX/FTxzrerTr6n7VaaIBku50aFJOB6E7Cw2ZFOXgkBUdgFZVafRlJ2cdHB0HIlJvk3vEMZD94=
---

# EconomyOverhaul

An [Elin](https://elin-game.com) mod that adds a persistent bank, an investable
market (stocks & real estate), and a loan system to the game world.

## Features

- **Bank account**: deposit / withdraw gold, daily compound interest.
- **Investment market**: buy and sell stocks and real estate through an
  in-game UI; prices follow a mean-reverting random walk with global
  inflation; daily income/expenses settle automatically into the bank.
- **Loan system**: borrow gold from the bank, repay partially or in full,
  with daily compound interest. Borrowing is capped by a configurable limit.
- **Localization**: full English / Spanish UI and messages
  (config `UI.Language` = `en` or `es`).
- **Save integration**: market and loan state persist next to the game save
  (Steam Cloud friendly). Legacy v0.25 saves are loaded and upgraded to the
  versioned schema; corrupt files are backed up and reset safely.

## Installation

1. Build and deploy with `build.ps1` (PowerShell):
   `powershell -ExecutionPolicy Bypass -File build.ps1`
2. The script builds `src\EconomyOverhaul.csproj` (netstandard2.0) and copies
   `EconomyOverhaul.dll` into the game's native Package folder:
   `<Elin>\Package\EconomyOverhaulMod\`.
3. Launch Elin. Toggle the bank window with the configured hotkey
   (default `F8`).

## Configuration

Settings live in `BepInEx\config\beinded.economyoverhaul.cfg`:

| Section | Key | Default | Meaning |
|---|---|---|---|
| Bank | DailyInterestRate | 0.001 | Daily interest rate (0.001 = 0.1%/day) |
| Bank | MinBalanceForInterest | 100 | Minimum balance to earn interest |
| Bank | NotifyOnInterest | true | Message when interest is credited |
| Bank | MaxTransactionAmount | 1000000 | Max single deposit/withdraw |
| Market | MarketDailyInflation | 0.0005 | Daily price inflation |
| Market | InvestNotifyDaily | true | Message on daily investment settlement |
| Loan | LoanMaxAmount | 100000 | Maximum outstanding loan |
| Loan | LoanDailyInterestRate | 0.002 | Daily loan interest rate |
| Loan | LoanNotifyDaily | true | Message when loan interest accrues |
| UI | ToggleBankUiKey | F8 | Key to open the bank UI |
| UI | Language | en | `en` or `es` |

All numeric settings are clamped to sane ranges in code.

## Repository layout

```
build.ps1                 Build + deploy script
src/                      C# source (netstandard2.0 BepInEx plugin)
  Banking/                Bank balance + interest service
  Core/                   ModConfig
  Investing/              AssetDef, MarketService, InvestmentSaveData
  Lang/                   ModText (EN/ES)
  Loans/                  LoanService, LoanSaveData
  Patches/                Harmony patches (save/load, day advance)
  UI/                     BankUI (IMGUI window)
docs/                     Phase design notes
Mod-Description.md        Steam Workshop description
```

## Changelog

### v0.26.0
- Fix: load market state from `Game.OnLoad` instead of `GameIO.LoadGame`
  (state now restores after the world is ready).
- Fix: bank/investment UI failures show a reason message instead of silently
  doing nothing.
- Fix: default daily interest rate corrected to 0.001 (0.1%/day).
- Fix: all numeric config values are clamped to sane ranges.
- Fix: investment settlement reports per-asset detail + net summary.
- New: Loan tab in the bank UI with borrow / repay / repay-all.
- New: portfolio total value and estimated daily net shown in market tabs.
- New: full EN/ES localization of UI and messages.
- New: versioned investment save schema with legacy migration and corrupt
  file backup.
- New: defensive save path handling.

### v0.25.0
- Market inflation, per-asset settlement, buy/sell UI tabs.

### v0.24.0
- Initial bank + investing implementation.
*（内容由AI生成，仅供参考）*
