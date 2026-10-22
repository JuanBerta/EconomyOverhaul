using BepInEx;
using BepInEx.Logging;
using EconomyOverhaul.Core;
using EconomyOverhaul.UI;
using HarmonyLib;
using UnityEngine;

namespace EconomyOverhaul;

/// <summary>
/// Entry point of the EconomyOverhaul BepInEx plugin.
/// Registered as a chain-loaded plugin inside the game's native Package system
/// (see PackageLoader) so it is loaded together with the game.
/// </summary>
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
	public const string Guid = "beinded.economyoverhaul";
	public const string Name = "EconomyOverhaul";
	public const string Version = "0.26.0";

	internal static ManualLogSource Log;
	internal static Plugin Instance;

	private void Awake()
	{
		Instance = this;
		Log = Logger;

		// Read all configurable values from BepInEx.cfg.
		ModConfig.Init(Config);

		// Apply all [HarmonyPatch] classes in this assembly.
		Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Guid);

		Log.LogInfo($"{Name} v{Version} loaded. " +
			$"Interest rate: {ModConfig.DailyInterestRate.Value * 100f:F3}% per day, " +
			$"min balance: {ModConfig.MinBalanceForInterest.Value}, " +
			$"UI key: {ModConfig.ToggleBankUiKey.Value}");
	}

	private void Start()
	{
		// The IMGUI bank window lives on its own persistent GameObject.
		GameObject go = new GameObject("EconomyOverhaul_BankUI");
		go.AddComponent<BankUI>();
		DontDestroyOnLoad(go);

		Log.LogInfo("Bank UI initialized. Press " + ModConfig.ToggleBankUiKey.Value + " to open.");
	}
}
