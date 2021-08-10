using System.IO;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;


namespace TNH_BGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	public class TNH_BGM_L : BaseUnityPlugin
	{
		public static ConfigEntry<float> bgmVolume;
		public static string tnh_bank_loc;
		public static string[] banks;
		public static string PluginsDir { get; } = Paths.PluginPath;

		public void Awake()
		{
			InitConfig();
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
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
		}

		public void InitConfig()
		{
			bgmVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 1.");
			bgmVolume.Value = Mathf.Clamp(bgmVolume.Value, 0, 1);
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
		public const string VERS = "1.1.0"; //surely this will be release ready!
	}
}