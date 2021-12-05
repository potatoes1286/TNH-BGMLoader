using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using FistVR;
using FMODUnity;
using UnityEngine;
using Stratum;
using Stratum.Extensions;
using TNH_BGLoader;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


namespace TNHBGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
	public class PluginMain : StratumPlugin
	{
		public static ConfigEntry<float> BackgroundMusicVolume;
		public static ConfigEntry<string> LastLoadedBank;
		public static string AssemblyDirectory { get {
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		public void Awake()
		{
			InitConfig();
			BankAPI.BankList = BankAPI.LegacyBanks.OrderBy(x => x).ToList();
			//nuke all duplicates
			BankAPI.BankList = BankAPI.BankList.Distinct().ToList();
			//the loader patch just checks for MX_TAH, not the full root path so this should bypass the check
			BankAPI.BankList.Add(Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"));
			//banks.Add("Surprise Me!");
			
			//get the bank last loaded and set banknum to it; if it doesnt exist it just defaults to 0
			for (int i = 0; i < BankAPI.BankList.Count; i++)
				if (Path.GetFileNameWithoutExtension(BankAPI.BankList[i]) == LastLoadedBank.Value) { BankAPI.BankIndex = i; break; }

			//patch yo things
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
		}

		public void InitConfig()
		{
			BackgroundMusicVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 4.");
			BackgroundMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 4);
			LastLoadedBank = Config.Bind("no touchy", "Saved Bank", "", "Not meant to be changed manually. This autosaves your last bank used, so you don't have to reset it every time you launch H3.");
		}

		
		
		//stratum loading
		public override void OnSetup(IStageContext<Empty> ctx) {
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
		}
		public Empty LoadTNHBankFile(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			if (!BankAPI.BankList.Contains(file.FullName))
			{
				BankAPI.BankList.Add(file.FullName);
			}
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
		public const string VERS = "1.5.4"; //surely this will be release ready!
	}
}