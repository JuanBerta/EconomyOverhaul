using EconomyOverhaul.Core;
using EconomyOverhaul.Lang;

namespace EconomyOverhaul.Banking;

/// <summary>
/// Applies daily compound interest to the player's bank balance.
/// Invoked once per in-game day from <see cref="GameDate.AdvanceDay"/>.
/// </summary>
public static class BankInterestService
{
	/// <summary>
	/// Credits interest for the current balance.
	/// Safe to call at any time; returns immediately when the game is not ready,
	/// the balance is below the configured minimum, or the computed interest is 0.
	/// </summary>
	public static void ApplyDailyInterest()
	{
		if (!BankService.IsReady)
		{
			return;
		}

		int balance = BankService.GetBalance();
		if (balance < ModConfig.MinBalanceForInterest.Value)
		{
			return;
		}

		// Simple daily interest; the next day compounds because the balance grew.
		int interest = (int)(balance * ModConfig.DailyInterestRate.Value);
		if (interest <= 0)
		{
			return;
		}

		EClass.game.cards.container_deposit.ModCurrency(interest);

		if (ModConfig.NotifyOnInterest.Value)
		{
			Msg.SayRaw(ModText.InterestMessage(interest, BankService.GetBalance()));
		}

		Plugin.Log.LogInfo($"Bank interest credited: +{interest} (balance {BankService.GetBalance()})");
	}
}
