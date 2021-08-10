using System.Collections.Generic;
using System.IO;
using FistVR;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;


namespace TNH_BGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	public class TNH_BGLoaderBepInExPlugin : BaseUnityPlugin
	{
		public static string tnh_bank_loc;
		private static string[] banks;
		public static string PluginsDir { get; } = Paths.PluginPath;

		public void Awake()
		{
			//get all banks
			banks = GetBanks();
			if (banks.Length > 1)
			{ // log if > 1 mod found
				Logger.LogError(banks.Length + " Take And Hold music replacers found! You can't play two BGs at once!");
			}
			foreach (var bank in banks)
			{ // list all mods found
				Logger.LogInfo("Found TNH Music Replacer " + Path.GetFileName(bank));
			}
			Harmony.CreateAndPatchAll(typeof(TNH_BGLoaderBepInExPlugin));
		}
		
		[HarmonyPatch(typeof(FVRFMODController), "Initialize")]
		[HarmonyPrefix]
		public static bool FVRFMODControllerPatch_Initialize(FVRFMODController __instance)
		{
			if (banks != null) {
				if (banks.Length >= 1) {
					Debug.Log("Injecting bank " + Path.GetFileName(banks[0]) + "into TNH!");
					__instance.BankPreload = banks[0];
				}
			}
			return true;
		}

		public string[] GetBanks()
		{
			Logger.LogInfo("Yoinking from" + PluginsDir);
			// surely this won't throw an access error!
			string[] banks = Directory.GetFiles(PluginsDir, "MX_TAH_*.bank", SearchOption.AllDirectories);
			// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
			return banks;
		}
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.tnhbgloader";
		public const string NAME = "Potatoes' TnH BG Loader";
		public const string VERS = "1.0.0"; //surely this will be release ready!
	}
}