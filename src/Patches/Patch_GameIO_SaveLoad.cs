using EconomyOverhaul.Investing;
using EconomyOverhaul.Loans;
using HarmonyLib;

namespace EconomyOverhaul.Patches;

/// <summary>
/// Keeps the investment market and loan system in sync with the game's save
/// system:
/// - On save, persist the market and loan snapshots next to the game save.
/// - On load, restore both snapshots for the loaded world. Hooks
///   <c>Game.OnLoad</c> instead of <c>GameIO.LoadGame</c> so the restore runs
///   after the game world is fully constructed.
/// - On new game, reset both systems to fresh defaults.
/// </summary>
[HarmonyPatch(typeof(GameIO), "SaveGame")]
public static class Patch_GameIO_SaveGame
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		MarketService.SaveNow();
		LoanService.SaveNow();
	}
}

[HarmonyPatch(typeof(Game), "OnLoad")]
public static class Patch_Game_OnLoad
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		MarketService.LoadFromDisk();
		LoanService.LoadFromDisk();
	}
}

[HarmonyPatch(typeof(Game), "StartNewGame")]
public static class Patch_Game_StartNewGame
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		MarketService.Reset();
		LoanService.Reset();
	}
}
