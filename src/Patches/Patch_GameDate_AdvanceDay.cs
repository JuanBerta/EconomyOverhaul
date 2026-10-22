using EconomyOverhaul.Banking;
using EconomyOverhaul.Investing;
using EconomyOverhaul.Loans;
using HarmonyLib;

namespace EconomyOverhaul.Patches;

/// <summary>
/// Advances the mod's per-day systems when the game clock rolls over:
/// bank interest, investment market tick, and loan interest.
/// </summary>
[HarmonyPatch(typeof(GameDate), "AdvanceDay")]
public static class Patch_GameDate_AdvanceDay
{
	[HarmonyPostfix]
	public static void Postfix()
	{
		BankInterestService.ApplyDailyInterest();
		MarketService.DailyTick();
		LoanService.ApplyDailyInterest();
	}
}
