using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using FistVR;
using FMODUnity;
using UnityEngine;
using Stratum;
using Stratum.Extensions;
using UnityEngine.UI;


namespace TNHBGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.SoftDependency)]
	//[BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
	public class TNHBackgroundMusicLoader : /*StratumPlugin*/ BaseUnityPlugin
	{
		public static ConfigEntry<float> BackgroundMusicVolume;
		public static ConfigEntry<string> LastLoadedBank;
		public static string TNHBankLocation;
		public static List<string> BankList = new List<string>();
		public static int BankIndex = 0;
		private static readonly string PLUGINS_DIR = Paths.PluginPath;
		public static string RelevantBank => BankList[BankIndex];
		public static bool BanksEmptyOrNull => (BankList == null || BankList.Count == 0);

		public void Awake()
		{
			InitConfig();
			BankList = LegacyBanks.OrderBy(x => x).ToList();
			//nuke all duplicates
			BankList = BankList.Distinct().ToList();
			//the loader patch just checks for MX_TAH, not the full root path so this should bypass the check
			BankList.Add($"{Application.streamingAssetsPath}/MX_TAH.bank");
			//banks.Add("Surprise Me!");
			
			//get the bank last loaded and set banknum to it; if it doesnt exist it just defaults to 0
			for (int i = 0; i < BankList.Count; i++)
				if (Path.GetFileNameWithoutExtension(BankList[i]) == LastLoadedBank.Value) { BankIndex = i; break; }

			//patch yo things
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
			//launch panel using sodalite
			try 
			{
				var panel = new TNHBackgroundMusicLoaderPanel(); // dont do this
			} catch 
			{
				Logger.LogWarning("Could not load PTNHBGML panel!");
			}
		}

		public void InitConfig()
		{
			BackgroundMusicVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 4.");
			BackgroundMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 4);
			LastLoadedBank = Config.Bind("no touchy", "Saved Bank", "", "Not meant to be changed manually. This autosaves your last bank used, so you don't have to reset it every time you launch H3.");
		}

		public List<string> LegacyBanks
		{
			get
			{
				// surely this won't throw an access error!
				var banks = Directory.GetFiles(PLUGINS_DIR, "MX_TAH_*.bank", SearchOption.AllDirectories).ToList();
				Logger.LogDebug(banks.Count + " banks loaded via legacy bank loader!");
				// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
				return banks;
			}
		}

		public static void SwapBank(int newBank)
		{
			//wrap around
			if (newBank <  0) newBank = 0;
			if (newBank >= BankList.Count) newBank = BankList.Count - 1;
			
			UnloadBankHard(RelevantBank); //force it to be unloaded
			BankIndex = newBank; //set banknum to new bank
			
			//set FMOD controller if it exists, otherwise simply load it
			RuntimeManager.LoadBank("MX_TAH"); //load new bank
			
			LastLoadedBank.Value = Path.GetFileNameWithoutExtension(RelevantBank);
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
		
		/*//stratum loading
		public override void OnSetup(IStageContext<Empty> ctx) {
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
		}

		public Empty LoadTNHBankFile(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			banks.Add(file.FullName);
			return new Empty();
		}

		public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx) {
			yield break;
		}*/
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "1.4.0"; //surely this will be release ready!
	}
}