namespace EconomyOverhaul.Loans;

/// <summary>
/// Serializable snapshot of the player's outstanding loan.
/// Persisted to the game save directory by <see cref="LoanService"/>.
/// Newtonsoft round-trippable POCO (same pattern as InvestmentSaveData).
/// </summary>
public sealed class LoanSaveData
{
	/// <summary>Save schema version (1 = current).</summary>
	public int Version { get; set; } = 1;

	/// <summary>Outstanding principal + accrued interest.</summary>
	public long LoanBalance { get; set; }

	/// <summary>Snapshot of the daily interest rate at borrow time.</summary>
	public float DailyRate { get; set; }

	/// <summary>Game day when the loan was taken (MarketService.DayCount).</summary>
	public int DayBorrowed { get; set; }
}
