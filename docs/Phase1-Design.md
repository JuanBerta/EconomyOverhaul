---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 89238829fb97168b9c4195c03c152aef_cd6d2e06a4d311f193c6525400f8a581
    ReservedCode1: HPltKWHCdlm+NbP7Ap9/Kd8I492wSofen5liS18xpCbKHAdrGrBKH4ILPIcg7cdJpcmB2RbxZFPLnyNYU/GPEDQCzF/WhZ0ngl6Ifeoi+652uTC0E/2P6COOuC0nRXj/ZhWGqOK9V/SIYffggX5trQsdcEXzT0uwEQ9rdwWLr677/4LWm0UJRGxuFjY=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 89238829fb97168b9c4195c03c152aef_cd6d2e06a4d311f193c6525400f8a581
    ReservedCode2: HPltKWHCdlm+NbP7Ap9/Kd8I492wSofen5liS18xpCbKHAdrGrBKH4ILPIcg7cdJpcmB2RbxZFPLnyNYU/GPEDQCzF/WhZ0ngl6Ifeoi+652uTC0E/2P6COOuC0nRXj/ZhWGqOK9V/SIYffggX5trQsdcEXzT0uwEQ9rdwWLr677/4LWm0UJRGxuFjY=
---

# EconomyOverhaul - Phase 1 Design Document

**Mod**: EconomyOverhaul
**Phase**: 1 - Banking System (deposit / withdraw / daily interest)
**Author**: Beinded (with Marvis as development agent)
**Version**: 0.24.0
**Date**: 2026-08-30

---

## 1. Goal

Deliver the first playable slice of the EconomyOverhaul vision: a **banking system**
where the player can deposit gold, withdraw it, and earn daily interest.
This phase validates the full toolchain (source reading -> C# -> Harmony -> DLL ->
game package) and establishes the project architecture that later phases will extend.

Phase 1 scope:

- Deposit gold from the player's pockets into the bank.
- Withdraw gold from the bank into the player's pockets.
- Automatic daily compound interest on the balance (configurable rate and minimum).
- In-game UI (IMGUI window, hotkey toggle, default F11).
- All settings exposed in BepInEx config.

Explicitly out of scope (later phases): loans, stocks, businesses, city economies,
trade caravans, migration.

---

## 2. Vanilla System Analysis

The game already contains a full banking-related foundation. Phase 1 reuses it
instead of inventing parallel state:

### 2.1 The deposit container

| Symbol | Location |
|---|---|
| `EClass.game.cards.container_deposit` | `CardManager` field, `Thing` |

- Declared as `public Thing container_deposit` in `CardManager`.
- Initialized at game start: if `player.bankMoney > 0`, it calls
  `Add("money", bankMoney)` to seed the container.
- `CardManager.cs` also keeps `player.bankMoney` for legacy/new-game seeding.
- Because `container_deposit` is a `Thing` in the game's card graph, it is
  **serialized by the game's save system** - balances survive saves for free.
  No custom serialization is needed in Phase 1.

### 2.2 Currency API (verified signatures)

| Member | Signature | Notes |
|---|---|---|
| `Card.ModCurrency` | `void ModCurrency(int a, string id = "money")` | +/- gold on a card/container |
| `Card.GetCurrency` | `int GetCurrency(string id = "money")` | Current gold amount |
| `Chara.TryPay` | `bool TryPay(int a, string id = "money")` | Deduct from player, returns success |
| `Card.Add` | `Thing Add(string id, int num = 1, int lv = 1)` | Add items (used for withdrawal payout) |
| `Msg.SayRaw` | `string SayRaw(string text)` | Show custom message in the game log |

### 2.3 The game clock

| Member | Location | Notes |
|---|---|---|
| `GameDate.AdvanceDay()` | `GameDate.cs` (`GameDate : Date`) | Called when a new in-game day starts |
| `GameDate.AdvanceHour()` | same | drives hour/day progression |

`AdvanceDay()` is the natural hook for daily interest settlement.

### 2.4 Vanilla UI precedent

The community mod **ElinAutoWork** (verified by decompilation) renders a full
management window with **Unity IMGUI** (`OnGUI` + `GUI.Window` + `GUILayout`)
from a `MonoBehaviour` on a `DontDestroyOnLoad` GameObject, toggled by a hotkey.
This approach requires **no prefabs/asset bundles**, making the mod a single DLL.

---

## 3. Architecture

Professional multi-file layout, one responsibility per class:

```
EconomyOverhaul/
├── build.ps1                    # backup -> build -> deploy pipeline
├── docs/
│   └── Phase1-Design.md         # this document
├── backup/                      # timestamped DLL backups
└── src/
    ├── EconomyOverhaul.csproj
    ├── Directory.Build.props    # GamePath definition
    ├── Plugin.cs                # BepInEx entry, Harmony patch-all, UI bootstrap
    ├── Core/
    │   └── ModConfig.cs         # all BepInEx ConfigEntry definitions
    ├── Banking/
    │   ├── BankService.cs       # deposit/withdraw/balance operations
    │   └── BankInterestService.cs  # daily interest settlement
    ├── Patches/
    │   └── Patch_GameDate_AdvanceDay.cs  # Harmony hook for the clock
    ├── UI/
    │   └── BankUI.cs            # IMGUI bank window
    └── Lang/
        └── ModText.cs           # centralized user-facing strings
```

### 3.1 Load mechanism

- The game uses **BepInEx 5** installed via doorstop (`winhttp.dll` +
  `doorstop_config.ini` in the game root).
- The native `Package` folder mods are bridged to BepInEx by **PackageLoader.dll**
  (chains DLLs under `Package/<Mod>/` as BepInEx plugins after
  `ModManager.IsInitialized`).
- Therefore the plugin is a standard `BaseUnityPlugin` with `[BepInPlugin]`
  (`beinded.economyoverhaul`) that ships inside `Package/EconomyOverhaulMod/`.

### 3.2 Harmony hooks

| Target | Patch | Purpose |
|---|---|---|
| `GameDate.AdvanceDay()` | Postfix | Credit daily interest |

Postfix (not Prefix) is used so interest applies after the game clock itself
advances; failure of interest logic never blocks the vanilla day transition.

---

## 4. Functional Specification

### 4.1 Deposit

- Input: amount (or "Deposit All").
- Preconditions: game ready, amount > 0, `EClass.pc.TryPay(amount)` succeeds.
- Effect: `container_deposit.ModCurrency(+amount)`.
- Feedback: `Msg.SayRaw` with new balance.

### 4.2 Withdraw

- Input: amount (or "Withdraw All").
- Preconditions: game ready, amount > 0, balance >= amount.
- Effect: `container_deposit.ModCurrency(-amount)` then `EClass.pc.Add("money", amount)`.
- Feedback: `Msg.SayRaw` with new balance.

### 4.3 Daily interest

- On every `GameDate.AdvanceDay()`:
  - Skip if balance < `MinBalanceForInterest`.
  - `interest = floor(balance * DailyInterestRate)`.
  - Skip if interest <= 0.
  - Credit `container_deposit.ModCurrency(+interest)`.
  - Optional message via `Msg.SayRaw` (config toggle).
- Compounding is implicit: next day's interest uses the grown balance.

### 4.4 UI

- IMGUI window, title "Economy Overhaul - Bank".
- Shows: balance, carried money, interest rate, min balance.
- Deposit / Withdraw text fields with buttons, plus Deposit All / Withdraw All.
- Hotkey toggle (default F11), Esc closes.
- Guards: hidden/disabled when `EClass.ui.BlockInput`; "Bank not available"
  fallback outside a game session.

### 4.5 Configuration (`BepInEx/config/beinded.economyoverhaul.cfg`)

| Section | Key | Default | Description |
|---|---|---|---|
| Bank | DailyInterestRate | 0.001 | 0.1% per day |
| Bank | MinBalanceForInterest | 100 | gold |
| Bank | NotifyOnInterest | true | show interest message |
| Bank | MaxTransactionAmount | 1000000 | per-operation cap |
| UI | ToggleBankUiKey | F11 | Unity KeyCode name |

---

## 5. Risk & Mitigation

| Risk | Mitigation |
|---|---|
| Save compatibility | Uses vanilla `container_deposit`; no custom state to corrupt |
| Interest exploits (park gold, AFK) | Rate is flat daily; balance cap via MaxTransactionAmount is per-op only; later phases may add diminishing returns |
| API drift on game updates | All touched APIs verified against local decompiled Base Files |
| Conflict with other bank mods (BankInterestMod exists) | Different plugin GUID, both may stack interest; documented, low impact |

---

## 6. Test Plan

1. Fresh save: open UI (F11), deposit 1000, verify balance + message.
2. Restart game: balance persists (container serialization).
3. Advance one day: interest credited (1000 @ 0.1% = +1 gold), message shown.
4. Withdraw partial amount; verify pockets + balance.
5. Withdraw more than balance: rejected with message.
6. Deposit more than carried: rejected.
7. Change config values (rate 0.01, min balance 500): restart, verify.
8. Esc closes UI; F11 toggles; UI does not appear on title screen input.
9. Old QoL automation features: previously backed-up DLL still restorable.

---

## 7. Future Phases (design seeds)

- **Phase 2 - Investment**: stocks/housing tracked as custom serializable state
  (introduce `[JsonProperty]` models + `SaveData` hook), price simulation service.
- **Phase 3 - Businesses**: extend `FactionBranch` income model, hireable manager AI.
- **Phase 4 - City economies / caravans / migration**: global simulation service
  ticked from `GameDate.AdvanceHour`, world-map caravan entities.
*（内容由AI生成，仅供参考）*
