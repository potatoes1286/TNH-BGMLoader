using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FistVR;
using TNH_BGLoader;
using UnityEngine;

namespace TNHBGLoader.Sosig
{
	//VoiceLine Set API.
	public class SosigVLSAPI : MonoBehaviour
	{
		//SosigManifests, SosigManifest Index, SosigManifest GUIDs, prefer GUIDs
		public static List<SosigManifest>        LoadedSosigVLSs       = new List<SosigManifest>();
		public static Dictionary<string, string> VLSGuidToName        = new Dictionary<string, string>();
		public static List<string>               VLSGuidOrder         = new List<string>();
		public static Dictionary<string, int>    CurrentSosigVLSIndex = new Dictionary<string, int>();


		public static SosigSpeechSet[] DefaultVLSs;
		
		public static int GetSosigVLSIndexFromGUID(string GUID) => LoadedSosigVLSs.IndexOf(GetManifestFromGUID(GUID));
		public static int GetSosigVLSIndexFromManifest(SosigManifest manifest) => LoadedSosigVLSs.IndexOf(manifest);
		public static SosigManifest GetManifestFromGUID(string GUID) => LoadedSosigVLSs.FindAll(a => a.guid == GUID).First();

		public static int CurrentSosigVlsIndexOfVlsSet(int index)
		{
			if(!CurrentSosigVLSIndex.ContainsKey(VLSGuidOrder[index]))
				PluginMain.DebugLog.LogFatal($"{VLSGuidOrder[index]} was not in the VLS index database!");
			return CurrentSosigVLSIndex[VLSGuidOrder[index]];
		}

		public static int CurrentSosigVlsIndexOfVlsSet(string guid)  => CurrentSosigVLSIndex[guid];
		public static SosigManifest CurrentSosigVlsOfVlsSet(int         index) => LoadedSosigVLSs[CurrentSosigVLSIndex[VLSGuidOrder[index]]];
		public static SosigManifest CurrentSosigVlsOfVlsSet(string      guid)  => LoadedSosigVLSs[CurrentSosigVLSIndex[guid]];
		public static Texture2D GetSosigVLSIcon(SosigManifest sosigVLS)
		{
			var pbase = Path.GetDirectoryName(sosigVLS.location);
			//assembles all the potential locations for the icon, in descending order of importance.
			string[] paths = new string[0];
			if (sosigVLS.guid == "h3vr.default")
				paths = new string[] { Path.Combine(PluginMain.AssemblyDirectory, "default/sosigvls_default.png") };
			else if (sosigVLS.guid == "h3vr.zosig")
				paths = new string[] { Path.Combine(PluginMain.AssemblyDirectory, "default/sosigvls_zosig.png") };
			else paths = new string[] { pbase + "/icon.png", Directory.GetParent(pbase) + "/icon.png" };
			
			return GeneralAPI.GetIcon(sosigVLS.guid, paths);
		}

		public static void SwapSosigVLS(string guid, string vlsSet)
		{
			if (guid == "ptnhbgml.random") guid = LoadedSosigVLSs[UnityEngine.Random.Range(1, LoadedSosigVLSs.Count)].guid;
			CurrentSosigVLSIndex[vlsSet] = GetSosigVLSIndexFromGUID(guid);
			//TODO: Fix this!
			//PluginMain.LastLoadedSosigVLS.Value = CurrentSosigVLS.guid;
		}
		
		public static SosigManifest GetManifestFromYamlfest(SosigYamlfest yamlfest) {
			yamlfest.VoiceLines = Path.Combine(Path.GetDirectoryName(yamlfest.Location), yamlfest.VoiceLines);
			SosigManifest manifest = new SosigManifest();
			//set speechset
			manifest.SpeechSet = ScriptableObject.CreateInstance<SosigSpeechSet>();
			manifest.SpeechSet.BasePitch = yamlfest.BasePitch;
			manifest.SpeechSet.BaseVolume = yamlfest.BaseVolume;
			manifest.SpeechSet.ForceDeathSpeech = yamlfest.ForceDeathSpeech;
			manifest.SpeechSet.UseAltDeathOnHeadExplode = yamlfest.UseAltDeathOnHeadExplode;
			manifest.SpeechSet.LessTalkativeSkirmish = yamlfest.LessTalkativeSkirmish;
			//set metadata
			manifest.name = yamlfest.Name;
			manifest.guid = yamlfest.GUID;
			manifest.location = yamlfest.Location;
			var files = Directory.GetFiles(yamlfest.VoiceLines, "*.wav", SearchOption.AllDirectories).ToList();
			files.AddRange(Directory.GetFiles(yamlfest.VoiceLines, "*.ogg", SearchOption.AllDirectories));
			InitializeLists(ref manifest.SpeechSet);
			foreach (var vl in files) //iterate through and handle all lines found
				AddNameToSpeechSet(ref manifest, Common.LoadClip(vl), Path.GetFileName(vl) + ", " + new FileInfo(vl).Directory.Name);
			return manifest;
		}
		
		public static AudioClip GetRandomPreview(string guid)
		{
			if (guid == "h3vr.default") return Common.LoadClip(Path.Combine(PluginMain.AssemblyDirectory, "default/sosigvls_default.wav"));
			var manifest = GetManifestFromGUID(guid);
			if (manifest.previews.Count == 0) return null;
			int rand = UnityEngine.Random.Range(0, manifest.previews.Count);
			return manifest.previews[rand];
		}

		public static void InitializeLists(ref SosigSpeechSet speechset)
		{
			#region pain
			speechset.OnJointBreak = new List<AudioClip>();
			speechset.OnJointSlice = new List<AudioClip>();
			speechset.OnJointSever = new List<AudioClip>();
			speechset.OnDeath = new List<AudioClip>();
			speechset.OnBackBreak = new List<AudioClip>();
			speechset.OnNeckBreak = new List<AudioClip>();
			speechset.OnPain = new List<AudioClip>();
			speechset.OnConfusion = new List<AudioClip>();
			speechset.OnDeathAlt = new List<AudioClip>();
			#endregion
			#region state
			speechset.OnWander = new List<AudioClip>();
			speechset.OnSkirmish = new List<AudioClip>();
			speechset.OnInvestigate = new List<AudioClip>();
			speechset.OnSearchingForGuns = new List<AudioClip>();
			speechset.OnTakingCover = new List<AudioClip>();
			speechset.OnBeingAimedAt = new List<AudioClip>();
			speechset.OnAssault = new List<AudioClip>();
			speechset.OnReloading = new List<AudioClip>();
			speechset.OnMedic = new List<AudioClip>();
			#endregion
			#region call/response
			speechset.OnCall_Skirmish = new List<AudioClip>();
			speechset.OnRespond_Skirmish = new List<AudioClip>();
			speechset.OnCall_Assistance = new List<AudioClip>();
			speechset.OnRespond_Assistance = new List<AudioClip>();
			#endregion
		}
		
		
		public static void AddNameToSpeechSet(ref SosigManifest manifest, AudioClip clip, string name)
		{
			if(clip == null) PluginMain.DebugLog.LogFatal("Cannot find clip for " + name + "!");
			#region pain
			if (name.Contains("pain_joint_break")) {manifest.SpeechSet.OnJointBreak.Add(clip); return;}
			if (name.Contains("pain_joint_slice")) {manifest.SpeechSet.OnJointSlice.Add(clip); return;}
			if (name.Contains("pain_joint_sever")) {manifest.SpeechSet.OnJointSever.Add(clip); return;}
			if (name.Contains("pain_death")) {manifest.SpeechSet.OnDeath.Add(clip); return;}
			if (name.Contains("pain_break_back")) {manifest.SpeechSet.OnBackBreak.Add(clip); return;}
			if (name.Contains("pain_break_neck")) {manifest.SpeechSet.OnNeckBreak.Add(clip); return;}
			if (name.Contains("pain_default")) {manifest.SpeechSet.OnPain.Add(clip); return;}
			if (name.Contains("pain_confusion")) {manifest.SpeechSet.OnConfusion.Add(clip); return;}
			if (name.Contains("pain_alt_death")) {manifest.SpeechSet.OnDeathAlt.Add(clip); return;}
			#endregion
			#region state
			if (name.Contains("state_wander")) {manifest.SpeechSet.OnWander.Add(clip); return;}
			if (name.Contains("state_skirmish")) {manifest.SpeechSet.OnSkirmish.Add(clip); return;}
			if (name.Contains("state_investigate")) {manifest.SpeechSet.OnInvestigate.Add(clip); return;}
			if (name.Contains("state_gunsearch")) {manifest.SpeechSet.OnSearchingForGuns.Add(clip); return;}
			if (name.Contains("state_takecover")) {manifest.SpeechSet.OnTakingCover.Add(clip); return;}
			if (name.Contains("state_aimedat")) {manifest.SpeechSet.OnBeingAimedAt.Add(clip); return;}
			if (name.Contains("state_assault")) {manifest.SpeechSet.OnAssault.Add(clip); return;}
			if (name.Contains("state_reload")) {manifest.SpeechSet.OnReloading.Add(clip); return;}
			if (name.Contains("state_medic")) {manifest.SpeechSet.OnMedic.Add(clip); return;}
			#endregion
			#region call/response
			if (name.Contains("call_skirmish")) {manifest.SpeechSet.OnCall_Skirmish.Add(clip); return;}
			if (name.Contains("respond_skirmish")) {manifest.SpeechSet.OnRespond_Skirmish.Add(clip); return;}
			if (name.Contains("call_assistance")) {manifest.SpeechSet.OnCall_Assistance.Add(clip); return;}
			if (name.Contains("respond_assistance")) {manifest.SpeechSet.OnRespond_Assistance.Add(clip); return;}
			if (name.Contains("example")) {manifest.previews.Add(clip); return;}
			if (name.Contains("unused")) return;
			#endregion
			PluginMain.DebugLog.LogError("voiceline " + name + " has no voiceline match!");
		}
	}
}