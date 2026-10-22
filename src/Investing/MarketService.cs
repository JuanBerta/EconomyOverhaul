using System.Collections.Generic;
using System.IO;
using EconomyOverhaul.Banking;
using EconomyOverhaul.Core;
using EconomyOverhaul.Lang;
using UnityEngine;

namespace EconomyOverhaul.Investing;

/// <summary>
/// Simulated exchange: owns the price state, the player's holdings, and the
/// daily settlement loop. Prices follow a mean-reverting random walk on top of
/// a global inflation trend. All money flows through the bank balance
/// (<c>container_deposit</c>), so the bank is the investment account.
/// </summary>
public static class MarketService
{
	private static readonly Dictionary<string, float> _factors = new Dictionary<string, float>();
	private static readonly Dictionary<string, int> _holdings = new Dictionary<string, int>();
	private static int _dayCount;

	/// <summary>
	/// Resolves the market save path defensively: never lets
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
			return dir + "economyoverhaul_invest.json";
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogWarning("Cannot resolve save path for investment market: " + ex.Message);
			return null;
		}
	}

	public static bool IsReady =>
		BankService.IsReady;

	public static int DayCount => _dayCount;

	/// <summary>Global inflation multiplier applied to every asset.</summary>
	public static float InflationFactor =>
		Mathf.Pow(1f + ModConfig.MarketDailyInflation.Value, _dayCount);

	public static float GetPrice(AssetDef asset) =>
		asset.BasePrice * InflationFactor * FactorOf(asset);

	public static int GetHolding(string assetId) =>
		_holdings.TryGetValue(assetId, out int held) ? held : 0;

	/// <summary>Total market value of all held assets of the given type.</summary>
	public static int GetTotalValue(AssetType type)
	{
		int total = 0;
		foreach (var kv in _holdings)
		{
			AssetDef asset = AssetDef.Find(kv.Key);
			if (asset == null || asset.Type != type || kv.Value <= 0)
			{
				continue;
			}
			total += (int)(GetPrice(asset) * kv.Value);
		}
		return total;
	}

	/// <summary>Estimated daily net income of all held assets of the given type.</summary>
	public static int GetEstDailyNet(AssetType type)
	{
		int total = 0;
		foreach (var kv in _holdings)
		{
			AssetDef asset = AssetDef.Find(kv.Key);
			if (asset == null || asset.Type != type || kv.Value <= 0)
			{
				continue;
			}
			total += (int)(kv.Value * GetPrice(asset) * (asset.IncomeRate - asset.ExpenseRate));
		}
		return total;
	}

	/// <summary>
	/// Advances the market by one game day: re-rolls every price factor and
	/// settles income/expenses on all held assets.
	/// </summary>
	public static void DailyTick()
	{
		_dayCount++;

		foreach (var asset in AssetDef.All)
		{
			float f = FactorOf(asset);
			float noise = Random.Range(-asset.Volatility, asset.Volatility);
			// Mean reversion toward 1 keeps prices near fundamentals.
			f = 1f + (f - 1f) * 0.85f + noise;
			f = Mathf.Clamp(f, 0.25f, 3.5f);
			_factors[asset.Id] = f;
		}

		SettleHoldings();
	}

	/// <summary>
	/// Buys <paramref name="qty"/> shares of an asset with bank gold.
	/// Returns false when the market/game is not ready, the asset is unknown,
	/// the quantity is invalid, or the bank cannot cover the cost.
	/// </summary>
	public static bool TryBuy(string assetId, int qty)
	{
		if (!IsReady || qty <= 0)
		{
			return false;
		}

		AssetDef asset = AssetDef.Find(assetId);
		if (asset == null)
		{
			return false;
		}

		int cost = (int)(GetPrice(asset) * qty);
		if (cost <= 0 || BankService.GetBalance() < cost)
		{
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(-cost);
		_holdings[assetId] = GetHolding(assetId) + qty;
		Msg.SayRaw(ModText.InvestBuyMessage(asset.Name, qty, cost, GetHolding(assetId)));
		SaveNow();
		return true;
	}

	/// <summary>Sells <paramref name="qty"/> shares back at the current price.</summary>
	public static bool TrySell(string assetId, int qty)
	{
		if (!IsReady || qty <= 0)
		{
			return false;
		}

		AssetDef asset = AssetDef.Find(assetId);
		int held = GetHolding(assetId);
		if (asset == null || held < qty)
		{
			return false;
		}

		int gain = (int)(GetPrice(asset) * qty);
		if (gain <= 0)
		{
			return false;
		}

		EClass.game.cards.container_deposit.ModCurrency(gain);
		int remaining = held - qty;
		if (remaining > 0)
		{
			_holdings[assetId] = remaining;
		}
		else
		{
			_holdings.Remove(assetId);
		}
		Msg.SayRaw(ModText.InvestSellMessage(asset.Name, qty, gain));
		SaveNow();
		return true;
	}

	/// <summary>
	/// Applies daily income (dividend/rent) minus expenses (maintenance/tax)
	/// for every held asset. Net positive credits the bank; net negative
	/// debits it but never below zero. Reports per-asset detail lines plus a
	/// net summary when notifications are enabled.
	/// </summary>
	private static void SettleHoldings()
	{
		if (!IsReady)
		{
			return;
		}

		int totalNet = 0;
		List<string> details = new List<string>();

		foreach (var kv in _holdings)
		{
			AssetDef asset = AssetDef.Find(kv.Key);
			if (asset == null || kv.Value <= 0)
			{
				continue;
			}

			float price = GetPrice(asset);
			int income = (int)(kv.Value * price * asset.IncomeRate);
			int expense = (int)(kv.Value * price * asset.ExpenseRate);
			int net = income - expense;
			if (net == 0)
			{
				continue;
			}

			if (net > 0)
			{
				EClass.game.cards.container_deposit.ModCurrency(net);
				totalNet += net;
			}
			else
			{
				int balance = BankService.GetBalance();
				int deduct = Mathf.Min(balance, -net);
				if (deduct > 0)
				{
					EClass.game.cards.container_deposit.ModCurrency(-deduct);
					totalNet -= deduct;
				}
				net = -deduct;
			}

			details.Add(asset.Name + ": " + (net > 0 ? "+" + net : net.ToString())
				+ " (income " + income + " - expense " + expense + ")");
		}

		if (details.Count > 0 && ModConfig.InvestNotifyDaily.Value)
		{
			foreach (string line in details)
			{
				Msg.SayRaw(line);
			}
		}

		if (totalNet != 0)
		{
			if (ModConfig.InvestNotifyDaily.Value)
			{
				Msg.SayRaw(ModText.InvestNetSummary(totalNet));
			}
			Plugin.Log.LogInfo($"Investment settlement: net {totalNet} gold (day {_dayCount}). Details: {string.Join(" | ", details)}");
		}
	}

	/// <summary>Writes the current market state to the save directory.</summary>
	public static void SaveNow()
	{
		string path = SafeSavePath();
		if (path == null)
		{
			Plugin.Log.LogWarning("Investment save skipped (save path unavailable).");
			return;
		}

		try
		{
			var data = new InvestmentSaveData
			{
				Version = 1,
				DayCount = _dayCount,
				Factors = new Dictionary<string, float>(_factors),
				Holdings = new Dictionary<string, int>(_holdings)
			};
			GameIO.SaveFile(path, data);
			Plugin.Log.LogInfo("Investment market saved to " + path);
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogWarning("Failed to save investment market: " + ex.Message);
		}
	}

	/// <summary>
	/// Loads market state from the save directory, if present.
	/// Legacy (Version 0) saves are accepted and upgraded on next save;
	/// newer versions are tolerated; corrupt files are backed up and reset.
	/// </summary>
	public static void LoadFromDisk()
	{
		Reset();

		string path = SafeSavePath();
		if (path == null)
		{
			Plugin.Log.LogWarning("Investment market load skipped (save path unavailable).");
			return;
		}

		if (!File.Exists(path))
		{
			Plugin.Log.LogInfo("No investment market save found; starting fresh market.");
			return;
		}

		try
		{
			InvestmentSaveData data = GameIO.LoadFile<InvestmentSaveData>(path);
			if (data == null)
			{
				return;
			}

			if (data.Version > 1)
			{
				Plugin.Log.LogWarning($"Investment save version {data.Version} is newer than supported (1); attempting tolerant load.");
			}

			_dayCount = data.DayCount;
			if (data.Factors != null)
			{
				foreach (var kv in data.Factors)
				{
					_factors[kv.Key] = kv.Value;
				}
			}
			if (data.Holdings != null)
			{
				foreach (var kv in data.Holdings)
				{
					_holdings[kv.Key] = kv.Value;
				}
			}
			Plugin.Log.LogInfo($"Investment market loaded: day {_dayCount}, {_holdings.Count} holdings.");
		}
		catch (System.Exception ex)
		{
			try
			{
				if (File.Exists(path))
				{
					string corrupt = path + ".corrupt";
					File.Copy(path, corrupt, true);
					Plugin.Log.LogWarning("Backed up corrupt investment save to " + corrupt);
				}
			}
			catch (System.Exception copyEx)
			{
				Plugin.Log.LogWarning("Failed to back up corrupt investment save: " + copyEx.Message);
			}
			Plugin.Log.LogWarning("Failed to load investment market, starting fresh: " + ex.Message);
			Reset();
		}
	}

	/// <summary>Resets the market to day 0 with default prices and no holdings.</summary>
	public static void Reset()
	{
		_dayCount = 0;
		_holdings.Clear();
		_factors.Clear();
		foreach (var asset in AssetDef.All)
		{
			_factors[asset.Id] = 1f;
		}
	}

	private static float FactorOf(AssetDef asset)
	{
		return _factors.TryGetValue(asset.Id, out float f) ? f : 1f;
	}
}
