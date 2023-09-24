using FistVR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FMOD;
using HarmonyLib;
using Microsoft.Win32;
using TNH_BGLoader;
using TNHBGLoader.Sosig;
using TNHBGLoader.Soundtrack;
using UnityEngine;
using UnityEngine.UI;
using Debug = FMOD.Debug;
using Random = UnityEngine.Random;
using System = FMOD.Studio.System;

namespace TNHBGLoader
{
	public class Patcher_FistVR
	{
		[HarmonyPatch(typeof(FistVR.Sosig), "Start")]
		[HarmonyPrefix]
		public static bool Sosig_Start_SetVLS(FistVR.Sosig __instance)
		{
			
			
			
			string sname = __instance.Speech.name;
			if (SosigVLSAPI.VLSGuidOrder.Contains(sname))
			{
				/*if (SosigVLSAPI.CurrentSosigVlsOfVlsSet(sname).guid == "h3vr.default"
				 && __instance.Speech.name != "SosigSpeech_Anton")
					__instance.Speech = def;
				if (SosigVLSAPI.CurrentSosigVlsOfVlsSet(sname).guid == "h3vr.zosig"
				 && __instance.Speech.name != "SosigSpeech_Zosig")
					__instance.Speech = zosig;*/
				if (SosigVLSAPI.CurrentSosigVlsOfVlsSet(sname).guid == "h3vr.default")
					return true;
				if (SosigVLSAPI.CurrentSosigVlsOfVlsSet(sname).guid == "h3vr.zosig")
					return true;
				__instance.Speech = SosigVLSAPI.CurrentSosigVlsOfVlsSet(sname).SpeechSet;
			}
			/*if (SosigVLSAPI.CurrentSosigVLS.guid == "h3vr.default") return true;
			if (__instance.Speech.name == "SosigSpeech_Anton") __instance.Speech = SosigVLSAPI.CurrentSosigVLS.SpeechSet;*/
			return true;
		}
		
		[HarmonyPatch(typeof(FVRFMODController), "SetMasterVolume")]
		[HarmonyPrefix]
		public static bool FVRFMODController_SetMasterVolume_IncludeBGMVol(ref float i) {
			i *= PluginMain.BackgroundMusicVolume.Value;
			return true;
		}
		
		[HarmonyPatch(typeof(SM), "PlayCoreSoundDelayed")]
		[HarmonyPrefix]
		public static bool SM_PlayCoreSoundDelayed_IncludeAnnouncerVol(ref AudioEvent ClipSet)
		{
			//makes the announcer conform to announcer volume
			ClipSet.VolumeRange.x *= PluginMain.AnnouncerMusicVolume.Value;
			ClipSet.VolumeRange.y *= PluginMain.AnnouncerMusicVolume.Value;
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "VoiceUpdate")]
		[HarmonyPrefix]
		public static bool TNH_Manager_VoiceUpdate_AccountForVoiceLinePadding(TNH_Manager __instance)
		{
			if (__instance.timeTilLineClear >= 0f)
			{
				__instance.timeTilLineClear -= Time.deltaTime;
				return true;
			}
			if (__instance.QueuedLines.Count > 0)
			{
				TNH_VoiceLineID key = __instance.QueuedLines.Dequeue();
				if (__instance.voiceDic_Standard.ContainsKey(key))
				{
					int index = UnityEngine.Random.Range(0, __instance.voiceDic_Standard[key].Count);
					AudioClip audioClip = __instance.voiceDic_Standard[key][index];
					AudioEvent audioEvent = new AudioEvent();
					audioEvent.Clips.Add(audioClip);
					audioEvent.PitchRange = new Vector2(1f, 1f);
					audioEvent.VolumeRange = new Vector2(0.6f, 0.6f);
					__instance.timeTilLineClear = audioClip.length + AnnouncerAPI.CurrentAnnouncer.BackPadding;
					SM.PlayCoreSoundDelayed(FVRPooledAudioType.UIChirp, audioEvent, __instance.transform.position, AnnouncerAPI.CurrentAnnouncer.FrontPadding);
				}
			}
			return true;
		}
		
		/*[HarmonyPatch(typeof(GM), "InitScene")]
		[HarmonyPrefix]
		public static bool GM_InitScene_CleanupSnippets(ref float i)
		{
			BankAPI
		}*/
		
		[HarmonyPatch(typeof(TNH_UIManager), "Start")]
		[HarmonyPostfix]
		public static void TNH_UIManager_SpawnPanel()
		{
			var bgmpanel = new TNHPanel();
			bgmpanel.Initialize("tnh", true, true, true);
			GameObject panel = bgmpanel.Panel.GetOrCreatePanel();
			panel.transform.position = new Vector3(0.0561f, 1f, 7.1821f);
			panel.transform.localEulerAngles = new Vector3(315, 0, 0);
			panel.GetComponent<FVRPhysicalObject>().SetIsKinematicLocked(true);
			//make rawimage ui thing
			var rawimage = new GameObject();
			var wait = rawimage.AddComponent<IconDisplayWaitForInit>();
			wait.panel = panel;
			wait.bgmpanel = bgmpanel;
			
			//get the bank last loaded and set banknum to it; if it doesnt exist it just defaults to 0
			if(!PluginMain.IsSoundtrack.Value)
				for (int i = 0; i < BankAPI.LoadedBankLocations.Count; i++)
					if (Path.GetFileNameWithoutExtension(BankAPI.LoadedBankLocations[i]) == PluginMain.LastLoadedBank.Value) { BankAPI.SwapBank(i); break; }
			
			if (PluginMain.IsSoundtrack.Value)
				SoundtrackAPI.EnableSoundtrackFromGUID(PluginMain.LastLoadedSoundtrack.Value);
			//set last loaded announcer
			AnnouncerAPI.CurrentAnnouncerIndex = AnnouncerAPI.GetAnnouncerIndexFromGUID(PluginMain.LastLoadedAnnouncer.Value);
			//set last loaded SosigVLS
			//TODO: FIX!
			//SosigVLSAPI.CurrentSosigVLSIndex = SosigVLSAPI.GetSosigVLSIndexFromGUID(PluginMain.LastLoadedSosigVLS.Value);
		}
		
		//Removes all remaining song snippets if game starts while a snippet is playing
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool TNH_Manager_Start_NukeSongSnippets()
		{
			BankAPI.NukeSongSnippets();
			return true;
		}
		
		//Loads all announcer lines into the DB. this should probably be couroutined... maybe????
		[HarmonyPatch(typeof(TNH_Manager), "InitLibraries")]
		[HarmonyPrefix]
		public static bool TNH_Manager_InitLibraries_LoadAnnouncers(TNH_Manager __instance)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			var announcer = AnnouncerAPI.CurrentAnnouncer;
			if (announcer.GUID == "h3vr.corrupted") {
				TNH_VoiceDatabase ddb = __instance.VoiceDB;
				foreach (var line in ddb.Lines) line.Clip_Standard = line.Clip_Corrupted;
				__instance.VoiceDB = ddb;
				return true;
			}
			
			if (announcer.GUID == "h3vr.default")
			{
				/*for(int i=0; i < __instance.VoiceDB.Lines.Count; i++) {
					SavWav.Save("G:/exp/" + __instance.VoiceDB.Lines[i].Clip_Standard.name + ".wav", __instance.VoiceDB.Lines[i].Clip_Standard);
					UnityEngine.Debug.Log(__instance.VoiceDB.Lines[i].Clip_Standard.name + " has ID " + (__instance.VoiceDB.Lines[i].ID).ToString());
				}*/
				return true;
			}
			TNH_VoiceDatabase db = ScriptableObject.CreateInstance<TNH_VoiceDatabase>();
			db.Lines = new List<TNH_VoiceDatabase.TNH_VoiceLine>();
			foreach (var line in announcer.VoiceLines)
			{
				//UnityEngine.Debug.Log("Loading ID " + line.ID);
				AudioClip sa = null;
				//i know there's a special place in hell for my naming scheme. dont care
				sa = AnnouncerAPI.GetAudioFromFile(line.ClipPath);
				if(sa == null) PluginMain.DebugLog.LogWarning("Failed to load from " + line.ClipPath + "!");
				var vl = new TNH_VoiceDatabase.TNH_VoiceLine();
				vl.ID = line.ID;
				vl.Clip_Standard = sa;
				db.Lines.Add(vl);
			}
			//UnityEngine.Debug.Log("Finished loading!");
			
			//if the announcermaker left out an ID, instead of leaving it blank, insert base game lines
			foreach (int vlid in Enum.GetValues(typeof(TNH_VoiceLineID)))
			{
				var linesofid = db.Lines.FindAll(line => line.ID == (TNH_VoiceLineID)vlid);
				if (linesofid.Count == 0)
				{
					//elegant but unreadable way to say "if voiceline name matches anything in UnusedVoiceLines"
					if(AnnouncerAPI.UnusedVoicelines.Any(((TNH_VoiceLineID)vlid).ToString().Contains))
						PluginMain.DebugLog.LogWarning("ID " + ((TNH_VoiceLineID)vlid).ToString() + " is empty for " + announcer.GUID +"! Was this intentional?");
					var baselinesofid = __instance.VoiceDB.Lines.FindAll(line => line.ID == (TNH_VoiceLineID)vlid);
					db.Lines = db.Lines.Concat(baselinesofid).ToList();
				}
			}
			sw.Stop();
			__instance.VoiceDB = db;
			PluginMain.LogSpam(sw.ElapsedMilliseconds + "ms to load all voicelines!");
			PluginMain.DebugLog.LogInfo("TNH run started! PTNHBGML Info:\nLoaded announcer: " + AnnouncerAPI.CurrentAnnouncer.GUID + "\nLoaded song: " + BankAPI.GetNameFromIndex(BankAPI.CurrentBankIndex));
			return true;
		}

		/*[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool TNH_ManagerPatch_Start()
		{
			if (TNH_BGM_L.relevantBank == "Surprise Me!")
			{
				int bankNum = Random.Range(0, TNH_BGM_L.banks.Count);
				TNH_BGM_L.SwapBank(bankNum);
				TNH_BGM_L.relevantBank = "Surprise Me!";
				TNH_BGM_L.lastLoadedBank.Value = "Surprise Me!";
			}
			return true;
		}*/
	}
}