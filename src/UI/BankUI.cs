using System;
using EconomyOverhaul.Banking;
using EconomyOverhaul.Core;
using EconomyOverhaul.Investing;
using EconomyOverhaul.Lang;
using EconomyOverhaul.Loans;
using UnityEngine;

namespace EconomyOverhaul.UI;

/// <summary>
/// Bank management window rendered with Unity IMGUI.
/// IMGUI was chosen because it requires no prefabs or asset bundles,
/// which keeps the mod trivially deployable (a single DLL).
/// Toggle with the configured hotkey (default F8).
/// Phase 2 added the Stocks and Real Estate market tabs.
/// v0.26 added the Loan tab, portfolio summary line, and EN/ES localization.
/// </summary>
public class BankUI : MonoBehaviour
{
	private enum BankTab
	{
		Bank,
		Stocks,
		RealEstate,
		Loan
	}

	public static BankUI Instance;

	private const int WindowId = 9877;

	private bool _visible;
	private Rect _windowRect;
	private string _depositText = "";
	private string _withdrawText = "";
	private string _borrowText = "";
	private string _repayText = "";
	private BankTab _tab = BankTab.Bank;
	private string _tradeQty = "1";
	private Vector2 _scrollPos;

	private void Awake()
	{
		Instance = this;
		DontDestroyOnLoad(gameObject);
		_windowRect = new Rect(200f, 90f, 620f, 480f);
	}

	private void Update()
	{
		if (!EClass.ui.BlockInput && Input.GetKeyDown(GetToggleKey()))
		{
			_visible = !_visible;
			if (_visible)
			{
				EInput.Consume(0);
			}
		}

		if (_visible && Input.GetKeyDown(KeyCode.Escape))
		{
			_visible = false;
		}

		// While the bank window is open, swallow left-click presses so the
		// game underneath (world selection, UI clicks) never receives them.
		// IMGUI buttons/fields consume their own events in OnGUI, so this
		// only blocks the pass-through, not window interaction.
		if (_visible && Input.GetMouseButtonDown(0))
		{
			EInput.Consume(0);
		}
	}

	private static KeyCode GetToggleKey()
	{
		return Enum.TryParse(ModConfig.ToggleBankUiKey.Value, true, out KeyCode key)
			? key
			: KeyCode.F11;
	}

	private void OnGUI()
	{
		if (!_visible)
		{
			return;
		}

		// ModalWindow keeps focus inside the window; outside clicks are not
		// forwarded to the game as selection (combined with Update's Consume).
		_windowRect = GUI.ModalWindow(WindowId, _windowRect, DrawWindow, ModText.UiTitle);
	}

	private void DrawWindow(int id)
	{
		if (!BankService.IsReady)
		{
			GUILayout.Label(ModText.BankNotAvailable);
			GUI.DragWindow();
			return;
		}

		GUILayout.Label(ModText.BankBalanceLabel + " " + BankService.GetBalance() + " gold");
		GUILayout.Space(6f);

		string[] tabs = { ModText.TabBank, ModText.TabStocks, ModText.TabRealEstate, ModText.TabLoan };
		int selected = GUILayout.Toolbar((int)_tab, tabs);
		_tab = (BankTab)selected;

		GUILayout.Space(6f);

		switch (_tab)
		{
			case BankTab.Bank:
				DrawBankTab();
				break;
			case BankTab.Stocks:
				DrawAssetTab(AssetType.Stock);
				break;
			case BankTab.RealEstate:
				DrawAssetTab(AssetType.RealEstate);
				break;
			case BankTab.Loan:
				DrawLoanTab();
				break;
		}

		GUI.DragWindow();
	}

	private void DrawBankTab()
	{
		int playerMoney = BankService.GetPlayerMoney();

		GUILayout.Label(ModText.CarriedMoneyLabel + " " + playerMoney + " gold");
		GUILayout.Label(ModText.DailyInterestLabel + " " + (ModConfig.DailyInterestRate.Value * 100f).ToString("F3") + "%");
		GUILayout.Label(ModText.MinBalanceLabel + " " + ModConfig.MinBalanceForInterest.Value + " gold");
		GUILayout.Space(10f);

		GUILayout.BeginHorizontal();
		GUILayout.Label(ModText.DepositLabel, GUILayout.Width(70f));
		_depositText = GUILayout.TextField(_depositText);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(ModText.DepositButton))
		{
			TryDeposit(_depositText);
		}
		if (GUILayout.Button(ModText.DepositAllButton))
		{
			BankService.DepositAll();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(6f);

		GUILayout.BeginHorizontal();
		GUILayout.Label(ModText.WithdrawLabel, GUILayout.Width(70f));
		_withdrawText = GUILayout.TextField(_withdrawText);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(ModText.WithdrawButton))
		{
			TryWithdraw(_withdrawText);
		}
		if (GUILayout.Button(ModText.WithdrawAllButton))
		{
			BankService.WithdrawAll();
		}
		GUILayout.EndHorizontal();
	}

	/// <summary>Draws the buy/sell list for one asset class (Stock or RealEstate).</summary>
	private void DrawAssetTab(AssetType type)
	{
		int totalValue = MarketService.GetTotalValue(type);
		int estDailyNet = MarketService.GetEstDailyNet(type);

		GUILayout.Label(
			ModText.DayLabel + " " + MarketService.DayCount
			+ " - " + ModText.InflationLabel + " x" + MarketService.InflationFactor.ToString("F3")
			+ " - " + ModText.BankBalanceLabel + " " + BankService.GetBalance() + " gold");
		GUILayout.Label(
			ModText.TotalValueLabel + " " + totalValue + " gold"
			+ "  |  " + ModText.EstDailyNetLabel + " " + (estDailyNet >= 0 ? "+" : "") + estDailyNet + " gold");

		GUILayout.BeginHorizontal();
		GUILayout.Label(ModText.QtyLabel, GUILayout.Width(40f));
		_tradeQty = GUILayout.TextField(_tradeQty, GUILayout.Width(70f));
		GUILayout.EndHorizontal();

		GUILayout.Space(4f);

		_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300f));
		foreach (var asset in AssetDef.All)
		{
			if (asset.Type != type)
			{
				continue;
			}

			int price = (int)MarketService.GetPrice(asset);
			int held = MarketService.GetHolding(asset.Id);

			GUILayout.BeginHorizontal();
			GUILayout.Label(
				asset.Name + "  " + price + "g  (" + (asset.IncomeRate * 100f).ToString("F1")
				+ "% in / " + (asset.ExpenseRate * 100f).ToString("F1") + "% out per day)  held: " + held,
				GUILayout.MinWidth(300f));

			if (GUILayout.Button(ModText.BuyButton, GUILayout.Width(56f)))
			{
				TryBuy(asset, _tradeQty);
			}
			if (GUILayout.Button(ModText.SellButton, GUILayout.Width(56f)))
			{
				TrySell(asset, _tradeQty);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
	}

	/// <summary>Draws the loan tab: balance, rate, and borrow/repay controls.</summary>
	private void DrawLoanTab()
	{
		long loan = LoanService.GetLoanBalance();
		float rate = LoanService.GetDailyRate();
		int dayBorrowed = LoanService.GetDayBorrowed();

		GUILayout.Label(ModText.LoanBalanceLabel + " " + loan + " gold");
		GUILayout.Label(ModText.LoanDailyRateLabel + " " + (rate * 100f).ToString("F3") + "%");
		GUILayout.Label(ModText.LoanMaxLabel + " " + ModConfig.LoanMaxAmount.Value + " gold");
		GUILayout.Label(ModText.DayBorrowedLabel + " " + (loan > 0 ? dayBorrowed.ToString() : "-"));
		GUILayout.Space(10f);

		GUILayout.BeginHorizontal();
		GUILayout.Label(ModText.LoanBorrowLabel, GUILayout.Width(70f));
		_borrowText = GUILayout.TextField(_borrowText);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(ModText.LoanBorrowButton))
		{
			TryBorrow(_borrowText);
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(6f);

		GUILayout.BeginHorizontal();
		GUILayout.Label(ModText.LoanRepayLabel, GUILayout.Width(70f));
		_repayText = GUILayout.TextField(_repayText);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(ModText.LoanRepayButton))
		{
			TryRepay(_repayText);
		}
		if (GUILayout.Button(ModText.LoanRepayAllButton))
		{
			LoanService.RepayAll();
		}
		GUILayout.EndHorizontal();
	}

	private static void TryBuy(AssetDef asset, string text)
	{
		if (!int.TryParse(text, out int qty) || qty <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!MarketService.TryBuy(asset.Id, qty))
		{
			Msg.SayRaw(ModText.InvestCannotAfford(asset.Name));
		}
	}

	private static void TrySell(AssetDef asset, string text)
	{
		if (!int.TryParse(text, out int qty) || qty <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!MarketService.TrySell(asset.Id, qty))
		{
			Msg.SayRaw(ModText.InvestNothingToSell(asset.Name));
		}
	}

	private static void TryDeposit(string text)
	{
		if (!int.TryParse(text, out int amount) || amount <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		int clamped = BankService.ClampToConfig(amount);
		if (clamped <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!BankService.Deposit(clamped))
		{
			Msg.SayRaw(ModText.NotEnoughMoney());
		}
	}

	private static void TryWithdraw(string text)
	{
		if (!int.TryParse(text, out int amount) || amount <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		int clamped = BankService.ClampToConfig(amount);
		if (clamped <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!BankService.Withdraw(clamped))
		{
			Msg.SayRaw(ModText.NotEnoughBalance());
		}
	}

	private static void TryBorrow(string text)
	{
		if (!int.TryParse(text, out int amount) || amount <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!LoanService.Borrow(amount))
		{
			Msg.SayRaw(ModText.LoanCannotBorrow());
		}
	}

	private static void TryRepay(string text)
	{
		if (!int.TryParse(text, out int amount) || amount <= 0)
		{
			Msg.SayRaw(ModText.InvalidAmount());
			return;
		}

		if (!LoanService.Repay(amount))
		{
			Msg.SayRaw(ModText.LoanCannotRepay());
		}
	}
}
