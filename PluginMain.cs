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
		public static List<string> BankList = new List<string>();
		public static int BankIndex = 0;
		private static readonly string PLUGINS_DIR = Paths.PluginPath;
		public static string loadedBank => BankList[BankIndex];
		public static bool BanksEmptyOrNull => (BankList == null || BankList.Count == 0);
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
			BankList = LegacyBanks.OrderBy(x => x).ToList();
			//nuke all duplicates
			BankList = BankList.Distinct().ToList();
			//the loader patch just checks for MX_TAH, not the full root path so this should bypass the check
			BankList.Add(Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"));
			//banks.Add("Surprise Me!");
			
			//get the bank last loaded and set banknum to it; if it doesnt exist it just defaults to 0
			for (int i = 0; i < BankList.Count; i++)
				if (Path.GetFileNameWithoutExtension(BankList[i]) == LastLoadedBank.Value) { BankIndex = i; break; }

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

		public List<string> LegacyBanks
		{
			get
			{
				// surely this won't throw an access error!
				var banks = Directory.GetFiles(PLUGINS_DIR, "MX_TAH_*.bank", SearchOption.AllDirectories).ToList();
				// removes all files with parent dir "resources"
				foreach (var bank in banks) if (Path.GetFileName(Path.GetDirectoryName(bank))?.ToLower() == "resources") BankList.Remove(bank);
				Logger.LogDebug(banks.Count + " banks loaded via legacy bank loader!");
				// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
				return banks;
			}
		}
		
		public delegate void bevent();
		public static event bevent OnBankSwapped;
		public static void SwapBank(int newBank)
		{
			//wrap around
			newBank = Mathf.Clamp(newBank, 0, BankList.Count - 1);
			UnloadBankHard(loadedBank); //force it to be unloaded
			BankIndex = newBank; //set banknum to new bank
			NukeSongSnippets();
			RuntimeManager.LoadBank("MX_TAH"); //load new bank (MX_TAH sets off the patcher)
			LastLoadedBank.Value = Path.GetFileNameWithoutExtension(loadedBank); //set last loaded bank
		}

		public static void NukeSongSnippets()
		{
			if (OnBankSwapped != null) OnBankSwapped(); //null moment!
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
		
		//stratum loading
		public override void OnSetup(IStageContext<Empty> ctx) {
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
		}

		public Empty LoadTNHBankFile(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			if (!BankList.Contains(file.FullName))
			{
				BankList.Add(file.FullName);
			}
			return new Empty();
		}
		
		//please co-routine this. doing this on the main thread is just asking for a freeze
		//granted it's 256x256 (usually), how hard can it be to load that?
		public static Texture2D LoadIconForBank(string bankName)
		{
			Debug.Log("Loading image for " + bankName);
			var pbase = Path.GetDirectoryName(bankName);
			string[] paths = new string[]
			{
				pbase + "/iconhq.png",
				Directory.GetParent(pbase) + "/iconhq.png",
				pbase + "/icon.png",
				Directory.GetParent(pbase) + "/icon.png"
			};
			if (bankName == Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"))
			{
				paths = new string[]
				{
					AssemblyDirectory + "/defaulticonhq.png"
				};
			}
			
			foreach(var path in paths)
			{
				if (File.Exists(path))
				{
					Debug.Log("Loading from " + path);
					//var tex = new WWW("file:///" + pbase + "iconhq.png").texture;
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1,1);
					tex.LoadImage(byteArray);
					if (tex != null)
					{
						Debug.Log("Loaded fine!");
						return tex;
					}
					else Debug.Log("Failed lo load!");
				} else Debug.Log(path + " does not exist!");
			}
			return null;
		}

		public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx) {
			yield break;
		}
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "1.4.4"; //surely this will be release ready!
	}
}