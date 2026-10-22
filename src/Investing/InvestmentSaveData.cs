using System.Collections.Generic;

namespace EconomyOverhaul.Investing;

/// <summary>
/// Serializable snapshot of the investment market plus the player's holdings.
/// Persisted to the game save directory by <see cref="MarketService"/>.
/// A plain POCO on purpose: Newtonsoft.Json (the game's serializer) can round
/// trip it through <c>GameIO.SaveFile</c> / <c>GameIO.LoadFile&lt;T&gt;</c>.
/// Version 1 is the current schema; Version 0 saves are legacy pre-0.26 files.
/// </summary>
public sealed class InvestmentSaveData
{
	/// <summary>Save schema version (0 = legacy, 1 = current).</summary>
	public int Version { get; set; } = 1;

	/// <summary>Number of market days elapsed (drives inflation).</summary>
	public int DayCount { get; set; }

	/// <summary>Per-asset price factors (id -> factor).</summary>
	public Dictionary<string, float> Factors { get; set; } = new Dictionary<string, float>();

	/// <summary>Per-asset shares owned by the player (id -> amount).</summary>
	public Dictionary<string, int> Holdings { get; set; } = new Dictionary<string, int>();
}
