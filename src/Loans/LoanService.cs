using System.IO;
using EconomyOverhaul.Banking;
using EconomyOverhaul.Core;
using EconomyOverhaul.Investing;
using EconomyOverhaul.Lang;

namespace EconomyOverhaul.Loans;

/// <summary>
/// Loan system: borrow gold from the bank (credited to the bank balance),
/// repay any amount or all at once, with daily compound interest.
/// Persisted per save as economyoverhaul_loan.json.
/// </summary>
public static class LoanService
{
	private static long _loanBalance;
	private static float _dailyRate;
	private static int _dayBorrowed;

	/// <summary>
	/// Resolves the loan save path defensively: never lets
	/// <c>GameIO.pathCurrentSave</c> throw or leak a null/empty path.
	/// </summary>
	private static string SafeSavePath()
	{
		try
		{
			string dir = GameIO.pathCurrentSave;
			if (string.IsNullOrEmpty(dir))
			{
				return null;
			}
			return dir + "economyoverhaul_loan.json";
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogWarning("Cannot resolve save path for loan system: " + ex.Message);
			return null;
		}
	}

	public static bool IsReady => BankService.IsReady;

	/// <summary>Outstanding loan balance (principal + accrued interest).</summary>
	public static long GetLoanBalance() => _loanBalance;

	/// <summary>Daily interest rate snapshot for the current loan.</summary>
	public static float GetDailyRate() => _dailyRate;

	/// <summary>Game day when the loan was taken (MarketService.DayCount).</summary>
	public static int GetDayBorrowed() => _dayBorrowed;

	/// <summary>
	/// Borrows gold: credits the bank balance and adds to the outstanding loan.
	/// Returns false when the game is not ready, the amount is invalid, or the
	/// resulting loan would exceed the configured maximum.
	/// </summary>
	public static bool Borrow(int amount)
	{
		if (!IsReady || amount <= 0)
		{
			return false;
		}

		long current = _loanBalance;
		if (current + amount > ModConfig.LoanMaxAmount.Value)
		{
			Msg.SayRaw(ModText.LoanCannotBorrow());
			return false;
		}

		// Fresh loan: snapshot the rate and the day it was taken.
		if (current == 0)
		{
			_dailyRate = ModConfig.LoanDailyInterestRate.Value;
			_dayBorrowed = MarketService.DayCount;
		}

		EClass.game.cards.container_deposit.ModCurrency(amount);
		_loanBalance = current + amount;
		Msg.SayRaw(ModText.LoanBorrowMessage(amount, BankService.GetBalance(), _loanBalance));
		SaveNow();
		return true;
	}

	/// <summary>
	/// Repays part of the loan from the bank balance.
	/// Returns false when the game is not ready, the amount is invalid, the bank
	/// balance is insufficient, or the loan balance is insufficient.
	/// </summary>
	public static bool Repay(int amount)
	{
		if (!IsReady || amount <= 0)
		{
			return false;
		}

		long current = _loanBalance;
		if (current <= 0 || current < amount)
		{
			Msg.SayRaw(ModText.LoanCannotRepay());
			return false;
		}

		if (BankService.GetBalance() < amount)
		{
			Msg.SayRaw(ModText.LoanCannotRepay());
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(-amount);
		_loanBalance = current - amount;
		if (_loanBalance == 0)
		{
			_dailyRate = 0f;
			_dayBorrowed = 0;
		}

		Msg.SayRaw(ModText.LoanRepayMessage(amount, _loanBalance));
		SaveNow();
		return true;
	}

	/// <summary>Repays the entire outstanding loan from the bank balance.</summary>
	public static bool RepayAll()
	{
		long current = _loanBalance;
		if (!IsReady || current <= 0)
		{
			Msg.SayRaw(ModText.LoanCannotRepay());
			return false;
		}

		if (BankService.GetBalance() < current)
		{
			Msg.SayRaw(ModText.LoanCannotRepay());
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(-(int)current);
		_loanBalance = 0;
		_dailyRate = 0f;
		_dayBorrowed = 0;
		Msg.SayRaw(ModText.LoanRepayAllMessage((int)current));
		SaveNow();
		return true;
	}

	/// <summary>Applies daily compound interest to the outstanding loan.</summary>
	public static void ApplyDailyInterest()
	{
		if (_loanBalance <= 0)
		{
			return;
		}

		long interest = (long)(_loanBalance * _dailyRate);
		if (interest > 0)
		{
			_loanBalance += interest;
			if (ModConfig.LoanNotifyDaily.Value)
			{
				Msg.SayRaw(ModText.LoanInterestMessage(interest, _loanBalance));
			}
			Plugin.Log.LogInfo($"Loan interest accrued: +{interest} (loan {_loanBalance})");
		}
	}

	/// <summary>Writes the current loan state to the save directory.</summary>
	public static void SaveNow()
	{
		string path = SafeSavePath();
		if (path == null)
		{
			Plugin.Log.LogWarning("Loan save skipped (save path unavailable).");
			return;
		}

		try
		{
			var data = new LoanSaveData
			{
				Version = 1,
				LoanBalance = _loanBalance,
				DailyRate = _dailyRate,
				DayBorrowed = _dayBorrowed
			};
			GameIO.SaveFile(path, data);
			Plugin.Log.LogInfo("Loan state saved to " + path);
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogWarning("Failed to save loan state: " + ex.Message);
		}
	}

	/// <summary>
	/// Loads loan state from the save directory, if present.
	/// Corrupt files are backed up (<c>.corrupt</c>) and reset safely.
	/// </summary>
	public static void LoadFromDisk()
	{
		Reset();

		string path = SafeSavePath();
		if (path == null)
		{
			Plugin.Log.LogWarning("Loan load skipped (save path unavailable).");
			return;
		}

		if (!File.Exists(path))
		{
			Plugin.Log.LogInfo("No loan save found; starting with no loan.");
			return;
		}

		try
		{
			LoanSaveData data = GameIO.LoadFile<LoanSaveData>(path);
			if (data == null)
			{
				return;
			}

			if (data.Version > 1)
			{
				Plugin.Log.LogWarning($"Loan save version {data.Version} is newer than supported (1); attempting tolerant load.");
			}

			_loanBalance = data.LoanBalance;
			_dailyRate = data.DailyRate;
			_dayBorrowed = data.DayBorrowed;
			Plugin.Log.LogInfo($"Loan state loaded: balance {_loanBalance}, day borrowed {_dayBorrowed}.");
		}
		catch (System.Exception ex)
		{
			try
			{
				if (File.Exists(path))
				{
					string corrupt = path + ".corrupt";
					File.Copy(path, corrupt, true);
					Plugin.Log.LogWarning("Backed up corrupt loan save to " + corrupt);
				}
			}
			catch (System.Exception copyEx)
			{
				Plugin.Log.LogWarning("Failed to back up corrupt loan save: " + copyEx.Message);
			}
			Plugin.Log.LogWarning("Failed to load loan state, resetting: " + ex.Message);
			Reset();
		}
	}

	/// <summary>Resets the loan to a no-debt state.</summary>
	public static void Reset()
	{
		_loanBalance = 0;
		_dailyRate = 0f;
		_dayBorrowed = 0;
	}
}
