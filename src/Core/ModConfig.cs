using BepInEx.Configuration;

namespace EconomyOverhaul.Core;

/// <summary>
/// Central definition of every user-tunable setting.
/// Values live in BepInEx\config\beinded.economyoverhaul.cfg and can be
/// edited while the game is closed. All numeric settings are clamped to sane
/// ranges so misconfiguration cannot break the mod or the save.
/// </summary>
public static class ModConfig
{
	/// <summary>Daily interest rate applied to the bank balance. 0.001 = 0.1% per day.</summary>
	public static ConfigEntry<float> DailyInterestRate { get; private set; }

	/// <summary>Minimum balance required to earn interest (avoids rounding noise on tiny accounts).</summary>
	public static ConfigEntry<int> MinBalanceForInterest { get; private set; }

	/// <summary>Show an in-game message whenever interest is credited.</summary>
	public static ConfigEntry<bool> NotifyOnInterest { get; private set; }

	/// <summary>Key used to toggle the bank UI (Unity KeyCode enum name, e.g. F11, B).</summary>
	public static ConfigEntry<string> ToggleBankUiKey { get; private set; }

	/// <summary>Upper bound for a single deposit/withdraw operation.</summary>
	public static ConfigEntry<int> MaxTransactionAmount { get; private set; }

	/// <summary>Daily inflation applied to every market price. 0.0005 = 0.05% per day (~20% per game year).</summary>
	public static ConfigEntry<float> MarketDailyInflation { get; private set; }

	/// <summary>Global multiplier for per-asset volatility (reserved tuning knob).</summary>
	public static ConfigEntry<float> MarketVolatility { get; private set; }

	/// <summary>Show an in-game message whenever investment holdings settle.</summary>
	public static ConfigEntry<bool> InvestNotifyDaily { get; private set; }

	/// <summary>Maximum outstanding loan balance allowed.</summary>
	public static ConfigEntry<int> LoanMaxAmount { get; private set; }

	/// <summary>Daily interest rate applied to the outstanding loan. 0.002 = 0.2% per day.</summary>
	public static ConfigEntry<float> LoanDailyInterestRate { get; private set; }

	/// <summary>Show an in-game message whenever loan interest accrues.</summary>
	public static ConfigEntry<bool> LoanNotifyDaily { get; private set; }

	/// <summary>UI language: "en" (English) or "es" (Spanish).</summary>
	public static ConfigEntry<string> Language { get; private set; }

	public static void Init(ConfigFile config)
	{
		DailyInterestRate = config.Bind(
			"Bank", "DailyInterestRate", 0.001f,
			new ConfigDescription(
				"Daily interest rate applied to the bank balance (0.001 = 0.1% per day).",
				new AcceptableValueRange<float>(0f, 0.1f)));

		MinBalanceForInterest = config.Bind(
			"Bank", "MinBalanceForInterest", 100,
			new ConfigDescription(
				"Minimum balance required to earn interest.",
				new AcceptableValueRange<int>(0, 100000000)));

		NotifyOnInterest = config.Bind(
			"Bank", "NotifyOnInterest", true,
			"Show a message whenever interest is credited.");

		ToggleBankUiKey = config.Bind(
			"UI", "ToggleBankUiKey", "F8",
			"Key used to toggle the bank UI (Unity KeyCode enum name, e.g. F8, B).");

		MaxTransactionAmount = config.Bind(
			"Bank", "MaxTransactionAmount", 1000000,
			new ConfigDescription(
				"Upper bound for a single deposit/withdraw operation.",
				new AcceptableValueRange<int>(1, 100000000)));

		MarketDailyInflation = config.Bind(
			"Market", "MarketDailyInflation", 0.0005f,
			new ConfigDescription(
				"Daily inflation applied to every market price (0.0005 = 0.05% per day).",
				new AcceptableValueRange<float>(0f, 0.01f)));

		MarketVolatility = config.Bind(
			"Market", "MarketVolatility", 0.03f,
			new ConfigDescription(
				"Global multiplier for per-asset price volatility (reserved tuning knob).",
				new AcceptableValueRange<float>(0f, 1f)));

		InvestNotifyDaily = config.Bind(
			"Market", "InvestNotifyDaily", true,
			"Show a message whenever investment holdings settle.");

		LoanMaxAmount = config.Bind(
			"Loan", "LoanMaxAmount", 100000,
			new ConfigDescription(
				"Maximum outstanding loan balance allowed.",
				new AcceptableValueRange<int>(0, 100000000)));

		LoanDailyInterestRate = config.Bind(
			"Loan", "LoanDailyInterestRate", 0.002f,
			new ConfigDescription(
				"Daily interest rate applied to the outstanding loan (0.002 = 0.2% per day).",
				new AcceptableValueRange<float>(0f, 0.1f)));

		LoanNotifyDaily = config.Bind(
			"Loan", "LoanNotifyDaily", true,
			"Show a message whenever loan interest accrues.");

		Language = config.Bind(
			"UI", "Language", "en",
			"UI language: en (English) or es (Spanish).");
	}
}
