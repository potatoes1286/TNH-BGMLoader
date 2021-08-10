using System;
using System.Collections.Generic;
using System.IO;
using FistVR;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;
using FMOD;
using FMODUnity;
using RootMotion.FinalIK;
using Debug = UnityEngine.Debug;


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

		[HarmonyPatch(typeof(RuntimeManager))]
		[HarmonyPatch("LoadBank", new Type[]{typeof(string), typeof(bool)})]
		[HarmonyPrefix]
		public static bool FMODRuntimeManagerPatch_LoadBank(ref string bankName)
		{
			if (bankName == "MX_TAH")
			{
				Debug.Log("Injecting bank " + Path.GetFileName(banks[0]) + " into TNH!");
				bankName = banks[0];
			}
			return true;
		}

		[HarmonyPatch(typeof(RuntimeUtils), "GetBankPath")]
		[HarmonyPrefix]
		public static bool FMODRuntimeUtilsPatch_GetBankPath(ref string bankName, ref string __result)
		{
			// 100% going to fucking strangle the person who didn't perchance even fucking THINk that
			// sOMEONE WOULD INSERT A FUCKIGN ABSOLUTE PATH LOCATION. HOW STUPID ARE YOU???
			// "waa waa the dev will only want banks to be loaded from streamingassets"
			// ARE YOU FIFTH GRADE??? OF COURSE THERE'S GONNA BE AN EDGE CASE HAVE YOU NEVER PROGRAMMED BEFORE??
			
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (Path.GetExtension(bankName) != ".bank")
			{
				if (Path.IsPathRooted(bankName)){
					__result = bankName + ".bank";
					return false;
				}
				else {
					__result = string.Format("{0}/{1}.bank", streamingAssetsPath, bankName);
					return false;
				}
			}

			if (Path.IsPathRooted(bankName)) {
				__result = bankName;
				return false;
			}
			else {
				__result = string.Format("{0}/{1}", streamingAssetsPath, bankName);
				return false;
			}
		}

		public string[] GetBanks()
		{
			Logger.LogInfo("Yoinking from " + PluginsDir);
			// surely this won't throw an access error!
			string[] banks = Directory.GetFiles(PluginsDir, "MX_TAH_*.bank", SearchOption.AllDirectories);
			// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
			return banks;
		}
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "1.0.0"; //surely this will be release ready!
	}
}