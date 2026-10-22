using EconomyOverhaul.Core;

namespace EconomyOverhaul.Lang;

/// <summary>
/// Centralized user-facing strings with full English/Spanish localization.
/// Every method returns the text for the language selected in the
/// <c>UI.Language</c> config ("en" or "es").
/// </summary>
public static class ModText
{
	/// <summary>True when the UI language is Spanish.</summary>
	public static bool IsSpanish => ModConfig.Language.Value == "es";

	private static string T(string en, string es) => IsSpanish ? es : en;

	// ---- Bank messages ----

	public static string DepositMessage(int amount, int balance) => T(
		"Deposited " + amount + " gold into the bank. Balance: " + balance + ".",
		"Depositado " + amount + " de oro en el banco. Saldo: " + balance + ".");

	public static string WithdrawMessage(int amount, int balance) => T(
		"Withdrew " + amount + " gold from the bank. Balance: " + balance + ".",
		"Retirado " + amount + " de oro del banco. Saldo: " + balance + ".");

	public static string InterestMessage(int interest, int balance) => T(
		"Your bank account earned " + interest + " gold in interest. Balance: " + balance + ".",
		"Tu cuenta bancaria ganó " + interest + " de oro en intereses. Saldo: " + balance + ".");

	public static string NotEnoughMoney() => T(
		"You do not have enough gold to deposit that amount.",
		"No tienes suficiente oro para depositar esa cantidad.");

	public static string NotEnoughBalance() => T(
		"The bank does not hold enough gold for that withdrawal.",
		"El banco no tiene suficiente oro para ese retiro.");

	public static string InvalidAmount() => T(
		"Invalid amount entered.",
		"Cantidad inválida ingresada.");

	// ---- Investment messages ----

	public static string InvestBuyMessage(string assetName, int qty, int cost, int held) => T(
		"Bought " + qty + "x " + assetName + " for " + cost + " gold. Holdings: " + held + ".",
		"Comprado " + qty + "x " + assetName + " por " + cost + " de oro. Tenencias: " + held + ".");

	public static string InvestSellMessage(string assetName, int qty, int gain) => T(
		"Sold " + qty + "x " + assetName + " for " + gain + " gold.",
		"Vendido " + qty + "x " + assetName + " por " + gain + " de oro.");

	public static string InvestSettlementMessage(int net) => T(
		net > 0
			? "Your investments earned " + net + " gold today."
			: "Your investments cost " + (-net) + " gold today.",
		net > 0
			? "Tus inversiones ganaron " + net + " de oro hoy."
			: "Tus inversiones costaron " + (-net) + " de oro hoy.");

	public static string InvestNetSummary(int net) => T(
		"Net today: " + (net > 0 ? "+" + net : net.ToString()) + " gold",
		"Balance neto de hoy: " + (net > 0 ? "+" + net : net.ToString()) + " de oro");

	public static string InvestCannotAfford(string assetName) => T(
		"You cannot afford to buy " + assetName + ".",
		"No puedes permitirte comprar " + assetName + ".");

	public static string InvestNothingToSell(string assetName) => T(
		"You do not hold enough " + assetName + " to sell.",
		"No tienes suficientes " + assetName + " para vender.");

	// ---- Loan messages ----

	public static string LoanBorrowMessage(int amount, int balance, long loan) => T(
		"Borrowed " + amount + " gold. Bank balance: " + balance + ". Loan balance: " + loan + ".",
		"Pediste prestado " + amount + " de oro. Saldo bancario: " + balance + ". Deuda: " + loan + ".");

	public static string LoanRepayMessage(int amount, long loan) => T(
		"Repaid " + amount + " gold of your loan. Remaining loan: " + loan + ".",
		"Pagaste " + amount + " de oro de tu préstamo. Deuda restante: " + loan + ".");

	public static string LoanRepayAllMessage(int amount) => T(
		"Your loan of " + amount + " gold was fully repaid.",
		"Tu préstamo de " + amount + " de oro fue pagado por completo.");

	public static string LoanInterestMessage(long interest, long loan) => T(
		"Your loan accrued " + interest + " gold in interest. Loan balance: " + loan + ".",
		"Tu préstamo acumuló " + interest + " de oro en intereses. Deuda: " + loan + ".");

	public static string LoanCannotBorrow() => T(
		"You cannot borrow that amount right now.",
		"No puedes pedir prestada esa cantidad ahora.");

	public static string LoanCannotRepay() => T(
		"You cannot repay that amount right now.",
		"No puedes pagar esa cantidad ahora.");

	// ---- UI labels ----

	public static string UiTitle => T("Economy Overhaul - Bank", "Economy Overhaul - Banco");

	public static string TabBank => T("Bank", "Banco");

	public static string TabStocks => T("Stocks", "Acciones");

	public static string TabRealEstate => T("Real Estate", "Bienes Raíces");

	public static string TabLoan => T("Loan", "Préstamo");

	public static string BankNotAvailable => T(
		"Bank not available yet. Enter a game session first.",
		"El banco aún no está disponible. Entra primero a una partida.");

	public static string BankBalanceLabel => T("Bank balance:", "Saldo bancario:");

	public static string CarriedMoneyLabel => T("Carried money:", "Oro en mano:");

	public static string DailyInterestLabel => T("Daily interest rate:", "Tasa de interés diaria:");

	public static string MinBalanceLabel => T("Min balance for interest:", "Saldo mínimo para interés:");

	public static string DayLabel => T("Day", "Día");

	public static string InflationLabel => T("inflation", "inflación");

	public static string TotalValueLabel => T("Total value:", "Valor total:");

	public static string EstDailyNetLabel => T("Est. daily net:", "Neto diario est.:");

	public static string DepositLabel => T("Deposit:", "Depositar:");

	public static string WithdrawLabel => T("Withdraw:", "Retirar:");

	public static string DepositButton => T("Deposit", "Depositar");

	public static string DepositAllButton => T("Deposit All", "Depositar Todo");

	public static string WithdrawButton => T("Withdraw", "Retirar");

	public static string WithdrawAllButton => T("Withdraw All", "Retirar Todo");

	public static string QtyLabel => T("Qty:", "Cant.:");

	public static string BuyButton => T("Buy", "Comprar");

	public static string SellButton => T("Sell", "Vender");

	public static string LoanBalanceLabel => T("Loan balance:", "Deuda:");

	public static string LoanDailyRateLabel => T("Loan daily rate:", "Tasa diaria del préstamo:");

	public static string LoanMaxLabel => T("Max borrow:", "Máx. préstamo:");

	public static string DayBorrowedLabel => T("Day borrowed:", "Día del préstamo:");

	public static string LoanBorrowLabel => T("Borrow:", "Pedir prestado:");

	public static string LoanRepayLabel => T("Repay:", "Pagar:");

	public static string LoanBorrowButton => T("Borrow", "Pedir");

	public static string LoanRepayButton => T("Repay", "Pagar");

	public static string LoanRepayAllButton => T("Repay All", "Pagar Todo");
}
