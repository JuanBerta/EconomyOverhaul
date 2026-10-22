using System.Collections.Generic;

namespace EconomyOverhaul.Investing;

/// <summary>Kind of investable asset.</summary>
public enum AssetType
{
	Stock,
	RealEstate
}

/// <summary>
/// Immutable market definition of a single investable asset.
/// The static <see cref="All"/> table is the game's fictional exchange.
/// </summary>
public sealed class AssetDef
{
	public string Id { get; }
	public string Name { get; }
	public AssetType Type { get; }
	public int BasePrice { get; }
	public float Volatility { get; }
	public float IncomeRate { get; }
	public float ExpenseRate { get; }

	public AssetDef(string id, string name, AssetType type, int basePrice,
		float volatility, float incomeRate, float expenseRate)
	{
		Id = id;
		Name = name;
		Type = type;
		BasePrice = basePrice;
		Volatility = volatility;
		IncomeRate = incomeRate;
		ExpenseRate = expenseRate;
	}

	/// <summary>The full fictional exchange listing.</summary>
	public static readonly IReadOnlyList<AssetDef> All = new List<AssetDef>
	{
		new AssetDef("stock_blacksmith", "Blacksmith Guild", AssetType.Stock, 500, 0.04f, 0.008f, 0.001f),
		new AssetDef("stock_farm", "Harvest Farm Co.", AssetType.Stock, 300, 0.03f, 0.006f, 0.002f),
		new AssetDef("stock_tavern", "Ale Tavern Inc.", AssetType.Stock, 400, 0.05f, 0.010f, 0.002f),
		new AssetDef("stock_adventurer", "Adventurer Hall", AssetType.Stock, 600, 0.06f, 0.007f, 0.001f),
		new AssetDef("stock_weaver", "Weaver Mill", AssetType.Stock, 350, 0.035f, 0.005f, 0.001f),
		new AssetDef("re_cottage", "Village Cottage", AssetType.RealEstate, 2000, 0.02f, 0.004f, 0.002f),
		new AssetDef("re_farm", "Suburban Farm", AssetType.RealEstate, 3500, 0.025f, 0.006f, 0.003f),
		new AssetDef("re_shop", "Town Shop Front", AssetType.RealEstate, 5000, 0.03f, 0.008f, 0.004f),
		new AssetDef("re_manor", "Noble Manor", AssetType.RealEstate, 12000, 0.04f, 0.010f, 0.006f),
	};

	public static AssetDef Find(string id)
	{
		foreach (var asset in All)
		{
			if (asset.Id == id)
			{
				return asset;
			}
		}
		return null;
	}
}
