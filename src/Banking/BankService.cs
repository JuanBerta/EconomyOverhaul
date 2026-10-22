using EconomyOverhaul.Core;
using EconomyOverhaul.Lang;

namespace EconomyOverhaul.Banking;

/// <summary>
/// Core banking operations backed by the game's built-in deposit container
/// (<c>EClass.game.cards.container_deposit</c>). Because the container is
/// serialized by the game, all balances survive saves/loads for free.
/// </summary>
public static class BankService
{
	/// <summary>True once a game session exists and the deposit container was created.</summary>
	public static bool IsReady =>
		EClass.game != null
		&& EClass.game.cards != null
		&& EClass.game.cards.container_deposit != null;

	/// <summary>Current bank balance in gold.</summary>
	public static int GetBalance()
	{
		return IsReady
			? EClass.game.cards.container_deposit.GetCurrency("money")
			: 0;
	}

	/// <summary>Gold currently carried by the player.</summary>
	public static int GetPlayerMoney()
	{
		return EClass.pc != null
			? EClass.pc.GetCurrency("money")
			: 0;
	}

	/// <summary>
	/// Moves gold from the player's pockets into the bank.
	/// Returns false when the player cannot afford the amount.
	/// </summary>
	public static bool Deposit(int amount)
	{
		if (!IsReady || amount <= 0 || !EClass.pc.TryPay(amount))
		{
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(amount);
		Msg.SayRaw(ModText.DepositMessage(amount, GetBalance()));
		return true;
	}

	/// <summary>
	/// Moves gold from the bank into the player's pockets.
	/// Returns false when the bank does not hold enough gold.
	/// </summary>
	public static bool Withdraw(int amount)
	{
		if (!IsReady || amount <= 0 || GetBalance() < amount)
		{
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(-amount);
		EClass.pc.Add("money", amount);
		Msg.SayRaw(ModText.WithdrawMessage(amount, GetBalance()));
		return true;
	}

	/// <summary>Convenience: move every carried gold coin into the bank.</summary>
	public static bool DepositAll()
	{
		int money = GetPlayerMoney();
		return money > 0 && Deposit(money);
	}

	/// <summary>Convenience: withdraw the entire balance.</summary>
	public static bool WithdrawAll()
	{
		int balance = GetBalance();
		return balance > 0 && Withdraw(balance);
	}

	/// <summary>Clamp an arbitrary input amount to the configured transaction bound.</summary>
	public static int ClampToConfig(int amount)
	{
		return amount <= 0 ? 0 : System.Math.Min(amount, ModConfig.MaxTransactionAmount.Value);
	}
}
