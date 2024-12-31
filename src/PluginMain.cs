using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
using TNHBGLoader.Sosig;
using TNHBGLoader.Soundtrack;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Logger = BepInEx.Logging.Logger;

namespace TNHBGLoader
{
	public static class Extensions {
		public static string ToOneLine<T>(this T[] list) {
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < list.Length; i++) {
				builder.Append(list[i].ToString());
				if (i != list.Length - 1)
					builder.Append(", ");
			}
			return builder.ToString();
		}
	}
	
	
	[BepInPlugin(PluginDetails.GUID, PluginDetails.NAME, PluginDetails.VERS)]
	[BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
	public class PluginMain : StratumPlugin
	{
		public static ConfigEntry<float>  BackgroundMusicVolume;
		public static ConfigEntry<float>  AnnouncerMusicVolume;
		public static ConfigEntry<string> LastLoadedBank;
		public static ConfigEntry<string> LastLoadedAnnouncer;
		public static ConfigEntry<string> LastLoadedSosigVLS;
		public static ConfigEntry<bool>   EnableDebugLogging;
		public static ConfigEntry<string> LastLoadedSoundtrack;
		public static ConfigEntry<bool>   IsSoundtrack;

		public static ConfigEntry<bool> LoadSoundtracksOnStartup;
		public static string AssemblyDirectory { get {
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		internal new static ManualLogSource DebugLog;
		public static void LogSpam(object data) { if(EnableDebugLogging.Value) DebugLog.LogInfo(data); }
		public void Awake()
		{
			DebugLog = Logger;
			InitConfig();
			
			//bank stuff
			BankAPI.LoadedBankLocations = BankAPI.GetLegacyBanks().OrderBy(x => x).ToList();
			//nuke all duplicates
			BankAPI.LoadedBankLocations = BankAPI.LoadedBankLocations.Distinct().ToList();
			
			BankAPI.LoadedBankLocations.Insert(0, "Select Random"); //if this is not first it can cause issues + don't rename this
			//BankAPI.LoadedBankLocations.Insert(1, "Your Mix"); //if this is not second it can cause issues + don't rename this

			//start YAML deserializer
			_deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
			//add default announcers
			AnnouncerAPI.LoadedAnnouncers.Add(AnnouncerManifest.RandomAnnouncer);
			AnnouncerAPI.LoadedAnnouncers.Add(AnnouncerManifest.DefaultAnnouncer);
			AnnouncerAPI.LoadedAnnouncers.Add(AnnouncerManifest.CorruptedAnnouncer);


			SosigVLSDefinitionSetLaod(new VLSGuidToNameDefinitionsYamlfest()
			{
				GuidsToNames = new []{"SosigSpeech_Anton:Default", "SosigSpeech_Zosig:Zosig"}
			});
			
			//add default sosig VLS
			SosigVLSAPI.LoadedSosigVLSs.Add(SosigManifest.RandomSosigVLS());
			SosigVLSAPI.LoadedSosigVLSs.Add(SosigManifest.DefaultSosigVLS());
			SosigVLSAPI.LoadedSosigVLSs.Add(SosigManifest.DefaultZosigVLS());

			//patch yo things
			Harmony.CreateAndPatchAll(typeof(Patcher_FMOD));
			Harmony.CreateAndPatchAll(typeof(Patcher_FistVR));
			Harmony.CreateAndPatchAll(typeof(TnHSoundtrackInterface));
		}
		
		//TODO: Move last loaded x into it's own storage file because bepinex config does not support string[] afaik
		public void InitConfig()
		{
			EnableDebugLogging = Config.Bind("General", "Enable Debug Logs", false, "Spams your log with debug info.");
			BackgroundMusicVolume = Config.Bind("General", "BGM Volume", 1f, "Changes the magnitude of the BGM volume. Must be between 0 and 4.");
			BackgroundMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 4);
			AnnouncerMusicVolume = Config.Bind("General", "Announcer Volume", 1f, "Changes the magnitude of the Announcer volume. Must be between 0 and 20. (Please don't set the volume to 2000%.)");
			AnnouncerMusicVolume.Value = Mathf.Clamp(BackgroundMusicVolume.Value, 0, 20);
			LastLoadedBank = Config.Bind("no touchy", "Saved Bank", "MX_TAH", "Not meant to be changed manually. This autosaves your last bank used, so you don't have to reset it every time you launch H3.");
			LastLoadedAnnouncer = Config.Bind("no touchy", "Saved Announcer", "h3vr.default", "Not meant to be changed manually. This autosaves your last announcer used, so you don't have to reset it every time you launch H3.");
			LastLoadedSosigVLS = Config.Bind("no touchy", "Saved Sosig VLS", "h3vr.default", "Not meant to be changed manually. This autosaves your last sosig set used, so you don't have to reset it every time you launch H3.");
			LastLoadedSoundtrack = Config.Bind("no touchy", "Saved Soundtrack", "", "Not meant to be changed manually. This autosaves your last sosig set used, so you don't have to reset it every time you launch H3.");
			IsSoundtrack = Config.Bind("no touchy", "Saved Is Soundtrack", false, "Not meant to be changed manually. This autosaves your last sosig set used, so you don't have to reset it every time you launch H3.");
			LoadSoundtracksOnStartup = Config.Bind("no touchy", "Load Soundtracks On Startup", false, "Debug. Loads ALL soundtracks on H3 start, not the current soundtrack when loading TnH. Enable only to catch soundtrack loading bugs on start.");
		}

		//stratum loading
		public override void OnSetup(IStageContext<Empty> ctx) {
			ctx.Loaders.Add("tnhbankfile", LoadTNHBankFile);
			ctx.Loaders.Add("tnhannouncer", LoadAnnouncer);
			ctx.Loaders.Add("tnhbgmlsosigvls", LoadSosigVoiceLineSet);
			ctx.Loaders.Add("tnhbgmlvlsdictionary", LoadSosigVoiceLineDefinitionSet);
			ctx.Loaders.Add("tnhbgml_soundtrack", LoadSoundtrack);
		}
		private IDeserializer _deserializer;
		public Empty LoadTNHBankFile(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			if (!BankAPI.LoadedBankLocations.Contains(file.FullName))
				BankAPI.LoadedBankLocations.Add(file.FullName);
			return new Empty();
		}
		
		public Empty LoadAnnouncer(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			var yamlfest = new AnnouncerYamlfest();
			yamlfest = _deserializer.Deserialize<AnnouncerYamlfest>(File.ReadAllText(file.FullName));
			DebugLog.LogInfo("Loaded announcer file " + yamlfest.GUID);
			yamlfest.Location = file.FullName;
			AnnouncerAPI.LoadedAnnouncers.Add(AnnouncerAPI.GetManifestFromYamlfest(yamlfest));
			return new Empty();
		}

		public Empty LoadSosigVoiceLineSet(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			var yamlfest = new SosigYamlfest();
			yamlfest = _deserializer.Deserialize<SosigYamlfest>(File.ReadAllText(file.FullName));
			DebugLog.LogInfo("Loaded Sosig Voiceline set " + yamlfest.GUID);
			yamlfest.Location = file.FullName;
			SosigVLSAPI.LoadedSosigVLSs.Add(SosigVLSAPI.GetManifestFromYamlfest(yamlfest));
			return new Empty();
		}
		
		public Empty LoadSosigVoiceLineDefinitionSet(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			VLSGuidToNameDefinitionsYamlfest yamlfest = _deserializer.Deserialize<VLSGuidToNameDefinitionsYamlfest>(File.ReadAllText(file.FullName));
			SosigVLSDefinitionSetLaod(yamlfest);
			return new Empty();
		}
		
		public Empty LoadSoundtrack(FileSystemInfo handle) {
			var file = handle.ConsumeFile();
			SoundtrackYamlfest yamlfest = _deserializer.Deserialize<SoundtrackYamlfest>(File.ReadAllText(file.FullName));
			DebugLog.LogInfo($"Soundtrack detected: {yamlfest.Name}");
			//Notably, we do not load the soundtrack here! (Performed by SoundtrackAPI.AssembleMusicData)
			//music mods can get STUPIDLY large. This fucks up your RAM.
			//Instead, we load it at TnH start.
			var manifest = yamlfest.ToManifest(file.FullName);
			if(LoadSoundtracksOnStartup.Value)
				manifest.AssembleMusicData(); // Unless we enable it manually.
			SoundtrackAPI.Soundtracks.Add(manifest);
			//if (yamlfest.Guid == LastLoadedSoundtrack.Value)
			//	SoundtrackAPI.SelectedSoundtrackIndex = SoundtrackAPI.Soundtracks.Count - 1;
			return new Empty();
		}
		
		//please refactor all these names. Lord help me.
		private void SosigVLSDefinitionSetLaod(VLSGuidToNameDefinitionsYamlfest yamlfest)
		{
			//DebugLog.LogInfo($"Loading {yamlfest.GuidsToNames.Length} VLS set definitions!");
			for (int i = 0; i < yamlfest.GuidsToNames.Length; i++)
			{
				//i love violating DRY!!!!
				string[] kvpair = yamlfest.GuidsToNames[i].Split(':');
				DebugLog.LogInfo("Loading Sosig Voiceline Dictionary Set defining: " + kvpair[0]);
				SosigVLSAPI.VLSGuidToName[kvpair[0]] = kvpair[1];
				SosigVLSAPI.CurrentSosigVLSIndex[kvpair[0]] = 1; //1: Default
				if(kvpair[0] == "SosigSpeech_Zosig")
					SosigVLSAPI.CurrentSosigVLSIndex[kvpair[0]] = 2; //2: Zosig
				if(!SosigVLSAPI.VLSGuidOrder.Contains(kvpair[0]))
					SosigVLSAPI.VLSGuidOrder.Add(kvpair[0]);
			}
		}

		public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx) { yield break; }
	}

	internal static class PluginDetails
	{
		public const string GUID = "dll.potatoes.ptnhbgml";
		public const string NAME = "Potatoes' Take And Hold Background Music Loader";
		public const string VERS = "4.1.3"; //surely this will be release ready!
	}
}