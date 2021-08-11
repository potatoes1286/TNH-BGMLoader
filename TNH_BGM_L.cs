using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using FMODUnity;
using UnityEngine;
using Stratum;
using Stratum.Extensions;
using UnityEngine.UI;


namespace TNH_BGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
	public class TNH_BGM_L : StratumPlugin
	{
		public static ConfigEntry<float> bgmVolume;
		public static string tnh_bank_loc;
		public static List<string> banks;
		public static int bankNum = 0;
		public static string PluginsDir { get; } = Paths.PluginPath;
		public static string relevantBank => banks[bankNum];
		public static bool areBanksEmptyOrNull => (banks == null || banks.Count == 0);

		public void Awake()
		{
			InitConfig();
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
			try
			{
				TNH_BGM_L_Panel uop = new TNH_BGM_L_Panel(); // dont do this
			}
			catch
			{
				Logger.LogWarning("Could not load PTNHBGML panel!");
			}
		}

		public void InitConfig()
		{
			bgmVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 1.");
			bgmVolume.Value = Mathf.Clamp(bgmVolume.Value, 0, 1);
		}
		
		/*public string[] GetBanks()
		{
			Logger.LogInfo("Yoinking from " + PluginsDir);
			// surely this won't throw an access error!
			string[] banks = Directory.GetFiles(PluginsDir, "MX_TAH_*.bank", SearchOption.AllDirectories);
			// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
			return banks;
		}*/

		public static void SwapBanks(int newBankNum)
		{
			Debug.Log("Swapping bank " + Path.GetFileName(relevantBank) + " for " + Path.GetFileName(banks[newBankNum]));
			UnloadBankHard(relevantBank);
			bankNum = newBankNum;
			RuntimeManager.LoadBank(relevantBank);
		}
		
		//literal copy of RuntimeManager.UnloadBank but hard unloads
		public static void UnloadBankHard(string bankName)
		{
			UnityEngine.Debug.Log("Hard unloading " + Path.GetFileName(bankName));
			RuntimeManager.LoadedBank value;
			if (RuntimeManager.Instance.loadedBanks.TryGetValue(bankName, out value))
			{
				value.RefCount = 0;
				value.Bank.unload();
				RuntimeManager.Instance.loadedBanks.Remove(bankName);
			}
		}

		public override void OnSetup(IStageContext<Empty> ctx)
		{
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
		}

		public Empty LoadTNHBankFile(FileSystemInfo handle)
		{
			var file = handle.ConsumeFile();
			banks.Add(file.FullName);
			return new Empty();
		}

		public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx) {
			yield break;
		}
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "1.1.0"; //surely this will be release ready!
	}
}