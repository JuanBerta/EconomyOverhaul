---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 89238829fb97168b9c4195c03c152aef_3982836dce5c11f1a475525400248c00
    ReservedCode1: EeKzWzPkCQxppfrRCAkFwldspDRqBWU/4CWw8nebPLGbjdm0Pmzc2Mpmg3lKSQ2vC4v18CYB4nFsHEo7r62gomt6rX+4x7pa9AzGzM2/tWzwynaK8rBZPInceHxd8Q690gvpqN5cH8ci0jipK8v3GN8JCcJsnmzgMl4ow6oTopcTDNYbpwSnVtViTfc=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 89238829fb97168b9c4195c03c152aef_3982836dce5c11f1a475525400248c00
    ReservedCode2: EeKzWzPkCQxppfrRCAkFwldspDRqBWU/4CWw8nebPLGbjdm0Pmzc2Mpmg3lKSQ2vC4v18CYB4nFsHEo7r62gomt6rX+4x7pa9AzGzM2/tWzwynaK8rBZPInceHxd8Q690gvpqN5cH8ci0jipK8v3GN8JCcJsnmzgMl4ow6oTopcTDNYbpwSnVtViTfc=
---

# Phase 3 Design — v0.26.0: Bug fixes, Localization, Loans & Save Robustness

Design notes for the EconomyOverhaul v0.26.0 update. See `Phase1-Design.md`
and `Phase2-Design.md` for the earlier phases.

## 1. Goals

1. Fix 5 known bugs from v0.25.
2. Add a **Loan** system (borrow / repay gold with daily interest).
3. Localize the whole UI and all mod messages in English and Spanish.
4. Make saves versioned, migratable, and robust against corruption.
5. Keep the single-DLL, netstandard2.0 BepInEx deployment model.

## 2. Bug fixes (v0.25 → v0.26)

| # | Bug | Root cause | Fix |
|---|---|---|---|
| 1 | Market state not loaded on load | `Patch_GameIO_LoadGame` postfix ran before the world was ready | Hook `Game.OnLoad` postfix instead |
| 2 | UI silently ignores bad input / failures | `TryDeposit`/`TryWithdraw`/`TryBuy`/`TrySell` returned void with no feedback | Show localized reason via `Msg.SayRaw` (`NotEnoughMoney`, `NotEnoughBalance`, `InvalidAmount`, etc.) |
| 3 | Interest rate default wrong | `DailyInterestRate` defaulted to 0.01 (1%/day, ~37x/year) | Default 0.001 (0.1%/day) |
| 4 | Unbounded config values | Raw `Bind` without range validation | Wrap every numeric setting in `AcceptableValueRange` |
| 5 | Settlement has no detail | `SettleHoldings` printed only a net total | Per-asset detail lines + net summary (localized) |

## 3. Loan system

### 3.1 Data model (`src/Loans/LoanSaveData.cs`)

```json
{
  "Version": 1,
  "LoanBalance": 5000,
  "DailyRate": 0.002,
  "DayBorrowed": 42
}
```

- `LoanBalance`: outstanding principal + accrued interest (`long`).
- `DailyRate`: rate snapshot at borrow time (stable for the life of the loan).
- `DayBorrowed`: `MarketService.DayCount` when the loan started (display only).

### 3.2 Service (`src/Loans/LoanService.cs`)

- `Borrow(amount)` — credits the bank balance, enforces
  `LoanMaxAmount`, snapshots rate/day on a fresh loan, saves.
- `Repay(amount)` / `RepayAll()` — debits the bank balance, enforces
  balance and outstanding checks, clears rate/day at zero, saves.
- `ApplyDailyInterest()` — daily compound interest, notified via
  `LoanNotifyDaily`.
- `SaveNow()` / `LoadFromDisk()` / `Reset()` — same pattern as
  `MarketService`; save file `economyoverhaul_loan.json` next to the game save.

### 3.3 UI (`src/UI/BankUI.cs`)

- New `BankTab.Loan`: balance, daily rate, max borrow, day borrowed,
  borrow field/button, repay field/button, repay-all button.

### 3.4 Config (`src/Core/ModConfig.cs`)

- `Loan.LoanMaxAmount` (default 100000, range [0, 100000000])
- `Loan.LoanDailyInterestRate` (default 0.002, range [0, 0.1])
- `Loan.LoanNotifyDaily` (default true)

### 3.5 Patches

- `Patch_GameDate_AdvanceDay` → `LoanService.ApplyDailyInterest()`.
- `Patch_GameIO_SaveLoad` → save/load/reset loan state alongside market.

## 4. Localization

- `src/Lang/ModText.cs` gains `IsSpanish` (reads `UI.Language` config) and a
  private `T(en, es)` helper. Every string method returns localized text.
- `BankUI` uses localized labels for window title, tabs, buttons, and status
  lines; asset names stay in English (proper nouns).

## 5. Save robustness (`src/Investing/*`)

- `InvestmentSaveData.Version` (1 = current; legacy saves have no field,
  deserialize as 0 and are treated as v0 — loaded and upgraded on next save).
- `LoadFromDisk` tolerates unknown future versions, backs up corrupt files
  to `<save>.corrupt`, then resets.
- `SafeSavePath()` wraps `GameIO.pathCurrentSave` in try/catch and skips
  save/load when the path is null/empty.
- Same robustness applied to the new loan save file.

## 6. Build & release

- `build.ps1` unchanged contract: backup installed DLL → `dotnet build -c
  Release` → deploy into `<Elin>\Package\EconomyOverhaulMod`.
- Version constants updated: `Plugin.Version = "0.26.0"`,
  `csproj <Version>0.26.0</Version>`.
- Repository initialized as git with a full descriptive commit for Steam
  Workshop upload.
*（内容由AI生成，仅供参考）*
