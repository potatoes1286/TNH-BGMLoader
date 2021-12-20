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
		public static List<SosigManifest> LoadedSosigVLS = new List<SosigManifest>();
		public static int CurrentSosigVLSIndex = 0;
		public static SosigManifest CurrentSosigVLS => LoadedSosigVLS[CurrentSosigVLSIndex];
		
		public static int GetSosigVLSIndexFromGUID(string GUID) => LoadedSosigVLS.IndexOf(GetManifestFromGUID(GUID));
		public static int GetSosigVLSIndexFromManifest(SosigManifest manifest) => LoadedSosigVLS.IndexOf(manifest);
		public static SosigManifest GetManifestFromGUID(string GUID) => LoadedSosigVLS.FindAll(a => a.guid == GUID).First();

		
		public static Texture2D GetSosigVLSIcon(SosigManifest sosigVLS)
		{
			var pbase = Path.GetDirectoryName(sosigVLS.location);
			//assembles all the potential locations for the icon, in descending order of importance.
			string[] paths = new string[0];
			if (sosigVLS.guid == "h3vr.default")
				paths = new string[] { Path.Combine(PluginMain.AssemblyDirectory, "default/sosigvls_default.png") };
			else paths = new string[] { pbase + "/icon.png", Directory.GetParent(pbase) + "/icon.png" };
			
			return GeneralAPI.GetIcon(sosigVLS.guid, paths);
		}

		public static void SwapSosigVLS(string guid)
		{
			CurrentSosigVLSIndex                 = GetSosigVLSIndexFromGUID(guid);
			PluginMain.LastLoadedAnnouncer.Value = CurrentSosigVLS.guid;
		}
		
		public static SosigManifest GetManifestFromYamlfest(SosigYamlfest yamlfest) {
			yamlfest.VoiceLines = Path.Combine(Path.GetDirectoryName(yamlfest.Location), yamlfest.VoiceLines);
			SosigManifest manifest = new SosigManifest();
			//set speechset
			manifest.SpeechSet = new SosigSpeechSet();
			manifest.SpeechSet.BasePitch = yamlfest.BasePitch;
			manifest.SpeechSet.BaseVolume = yamlfest.BaseVolume;
			manifest.SpeechSet.ForceDeathSpeech = yamlfest.ForceDeathSpeech;
			manifest.SpeechSet.UseAltDeathOnHeadExplode = yamlfest.UseAltDeathOnHeadExplode;
			manifest.SpeechSet.LessTalkativeSkirmish = yamlfest.LessTalkativeSkirmish;
			//set metadata
			manifest.name = yamlfest.Name;
			manifest.guid = yamlfest.GUID;
			manifest.location = yamlfest.Location;
			manifest.previews = Directory.GetFiles(yamlfest.VoiceLines, "example*.wav", SearchOption.AllDirectories).ToList();
			var files = Directory.GetFiles(yamlfest.VoiceLines, "*.wav", SearchOption.AllDirectories).ToList();
			foreach (var song in files) //iterate through and handle all lines found
				AddNameToSpeechSet(ref manifest.SpeechSet, GetAudioFromFile(song), Path.GetFileName(song));
			return manifest;
		}
		
		public static AudioClip GetRandomPreview(string guid)
		{
			if (guid == "h3vr.default") return GetAudioFromFile(Path.Combine(PluginMain.AssemblyDirectory, "default/sosigvls_default.wav"));
			var manifest = GetManifestFromGUID(guid);
			int rand = UnityEngine.Random.Range(0, manifest.previews.Count);
			return GetAudioFromFile(manifest.previews[rand]);
		}
		
		public static void AddNameToSpeechSet(ref SosigSpeechSet speechset, AudioClip clip, string name)
		{
			//PAIN
			if (name.Contains("pain_joint_break")) speechset.OnJointBreak.Add(clip);
			if (name.Contains("pain_joint_slice")) speechset.OnJointSlice.Add(clip);
			if (name.Contains("pain_joint_sever")) speechset.OnJointSever.Add(clip);
			if (name.Contains("pain_death")) speechset.OnDeath.Add(clip);
			if (name.Contains("pain_break_back")) speechset.OnBackBreak.Add(clip);
			if (name.Contains("pain_break_neck")) speechset.OnNeckBreak.Add(clip);
			if (name.Contains("pain_default")) speechset.OnPain.Add(clip);
			if (name.Contains("pain_confusion")) speechset.OnConfusion.Add(clip);
			if (name.Contains("pain_alt_death")) speechset.OnDeathAlt.Add(clip);
			//STATE
			if (name.Contains("state_wander")) speechset.OnWander.Add(clip);
			if (name.Contains("state_skirmish")) speechset.OnSkirmish.Add(clip);
			if (name.Contains("state_investigate")) speechset.OnInvestigate.Add(clip);
			if (name.Contains("state_gunsearch")) speechset.OnSearchingForGuns.Add(clip);
			if (name.Contains("state_takecover")) speechset.OnTakingCover.Add(clip);
			if (name.Contains("state_aimedat")) speechset.OnBeingAimedAt.Add(clip);
			if (name.Contains("state_assault")) speechset.OnAssault.Add(clip);
			if (name.Contains("state_reload")) speechset.OnReloading.Add(clip);
			if (name.Contains("state_medic")) speechset.OnMedic.Add(clip);
			//CALL AND RESPONSE
			if (name.Contains("call_skirmish")) speechset.OnCall_Skirmish.Add(clip);
			if (name.Contains("respond_skirmish")) speechset.OnRespond_Skirmish.Add(clip);
			if (name.Contains("call_assistance")) speechset.OnCall_Assistance.Add(clip);
			if (name.Contains("respond_assistance")) speechset.OnRespond_Assistance.Add(clip);
		}
		
		//why does this field even exist lol
		public static AudioClip GetAudioFromFile(string path) => WavUtility.ToAudioClip(path);
	}
}