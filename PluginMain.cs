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
using BepInEx.Logging;
using FistVR;
using FMODUnity;
using UnityEngine;
using Stratum;
using Stratum.Extensions;
using TNH_BGLoader;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Logger = BepInEx.Logging.Logger;

namespace TNHBGLoader
{
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
	public class PluginMain : StratumPlugin
	{
		public static ConfigEntry<float> BackgroundMusicVolume;
		public static ConfigEntry<float> AnnouncerMusicVolume;
		public static ConfigEntry<string> LastLoadedBank;
		public static ConfigEntry<string> LastLoadedAnnouncer;
		public static ConfigEntry<bool> EnableDebugLogging;
		public static string AssemblyDirectory { get {
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}
		
		internal new static ManualLogSource DebugLog;
		public static void LogSpam(object data) { if(EnableDebugLogging.Value) DebugLog.LogDebug(data); }
		public void Awake()
		{
			DebugLog = Logger;
			InitConfig();
			
			//bank stuff
			BankAPI.BankLocations = BankAPI.LegacyBanks.OrderBy(x => x).ToList();
			//nuke all duplicates
			BankAPI.BankLocations = BankAPI.BankLocations.Distinct().ToList();
			//the loader patch just checks for MX_TAH, not the full root path so this should bypass the check
			BankAPI.BankLocations.Add(Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"));
			//banks.Add("Surprise Me!");
			
			//announcer schtuff
			_deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
			AnnouncerAPI.Announcers.Add(AnnouncerManifest.DefaultAnnouncer);
			AnnouncerAPI.Announcers.Add(AnnouncerManifest.CorruptedAnnouncer);
			
			
			//patch yo things
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
		}

		public void InitConfig()
		{
			EnableDebugLogging = Config.Bind("General", "Enable Debug Logs", false, "Spams your log with debug info.");
			BackgroundMusicVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 4.");
			BackgroundMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 4);
			AnnouncerMusicVolume = Config.Bind("General", "Announcer Volume", 1f, "Changes the magnitude of the Announcer volume. Must be between 0 and 20. (Please don't set the volume to 2000%.)");
			AnnouncerMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 20);
			LastLoadedBank = Config.Bind("no touchy", "Saved Bank", "", "Not meant to be changed manually. This autosaves your last bank used, so you don't have to reset it every time you launch H3.");
			LastLoadedAnnouncer = Config.Bind("no touchy", "Saved Announcer", "", "Not meant to be changed manually. This autosaves your last announcer used, so you don't have to reset it every time you launch H3.");
		}

		//stratum loading
		public override void OnSetup(IStageContext<Empty> ctx) {
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
			ctx.Loaders.Add("tnhannouncer", LoadAnnouncer);
		}
		public Empty LoadTNHBankFile(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			if (!BankAPI.BankLocations.Contains(file.FullName))
				BankAPI.BankLocations.Add(file.FullName);
			return new Empty();
		}

		private IDeserializer _deserializer;
		public Empty LoadAnnouncer(FileSystemInfo handle)
		{
			var file = handle.ConsumeFile();
			var yamlfest = new AnnouncerYamlfest();
			yamlfest = _deserializer.Deserialize<AnnouncerYamlfest>(File.ReadAllText(file.FullName));
			Debug.Log("Loaded announcer file " + yamlfest.GUID);
			yamlfest.Location = file.FullName;
			AnnouncerAPI.Announcers.Add(AnnouncerAPI.YamlfestToManifest(yamlfest));
			return new Empty();
		}
		public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx) { yield break; }
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "2.0.0"; //surely this will be release ready!
	}
}