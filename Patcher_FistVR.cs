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
using UnityEngine;
using UnityEngine.UI;
using Debug = FMOD.Debug;
using Random = UnityEngine.Random;
using System = FMOD.Studio.System;

namespace TNHBGLoader
{
	public class Patcher_FistVR
	{
		[HarmonyPatch(typeof(FVRFMODController), "SetMasterVolume")]
		[HarmonyPrefix]
		public static bool FVRFMODController_SetMasterVolume_IncludeBGMVol(ref float i)
		{
			i *= PluginMain.BackgroundMusicVolume.Value;
			return true;
		}
		
		[HarmonyPatch(typeof(SM), "PlayCoreSoundDelayed")]
		[HarmonyPrefix]
		public static bool SM_PlayCoreSoundDelayed_IncludeAnnouncerVol(ref AudioEvent ClipSet)
		{
			ClipSet.VolumeRange.x *= PluginMain.BackgroundMusicVolume.Value;
			ClipSet.VolumeRange.y *= PluginMain.BackgroundMusicVolume.Value;
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_UIManager), "Start")]
		[HarmonyPostfix]
		public static void TNH_UIManager_SpawnPanel()
		{
			var bgmpanel = new TNHPanel();
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
			for (int i = 0; i < BankAPI.BankLocations.Count; i++)
				if (Path.GetFileNameWithoutExtension(BankAPI.BankLocations[i]) == PluginMain.LastLoadedBank.Value) { BankAPI.LoadedBankIndex = i; break; }
			//set last loaded announcer
			AnnouncerAPI.LoadedAnnouncerIndex = AnnouncerAPI.GetAnnouncerIndexFromGUID(PluginMain.LastLoadedAnnouncer.Value);
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
			if (announcer.GUID == "h3vr.default")
			{
				TNH_VoiceDatabase ddb = __instance.VoiceDB;
				foreach (var line in ddb.Lines) line.Clip_Standard = line.Clip_Corrupted;
				__instance.VoiceDB = ddb;
				return true;
			}
			TNH_VoiceDatabase db = ScriptableObject.CreateInstance<TNH_VoiceDatabase>();
			db.Lines = new List<TNH_VoiceDatabase.TNH_VoiceLine>();
			foreach (var line in announcer.VoiceLines)
			{
				//UnityEngine.Debug.Log("Loading ID " + line.ID);
				AudioClip sa = null;
				AudioClip ca = null;
				//i know there's a special place in hell for my naming scheme. dont care
				if (!string.IsNullOrEmpty(line.StandardAudioClipPath))
					sa = AnnouncerAPI.GetAudioFromFile(line.StandardAudioClipPath);
				if (!string.IsNullOrEmpty(line.CorruptedAudioClipPath))
					ca = AnnouncerAPI.GetAudioFromFile(line.CorruptedAudioClipPath);
				if(sa == null) UnityEngine.Debug.LogWarning("SA is missing!");
				if(ca == null && announcer.HasCorruptedVer) UnityEngine.Debug.LogWarning("CA is missing!");
				var vl = new TNH_VoiceDatabase.TNH_VoiceLine();
				vl.ID = line.ID;
				if (!PluginMain.EnableCorruptedAnnouncer.Value) {
					vl.Clip_Standard = sa;
					vl.Clip_Corrupted = ca;
				} else {
					vl.Clip_Standard = ca;
					vl.Clip_Corrupted = sa;
				}
				
				db.Lines.Add(vl);
			}
			//UnityEngine.Debug.Log("Finished loading!");
			
			//if the announcermaker left out an ID, instead of leaving it blank, insert base game lines
			foreach (int vlid in Enum.GetValues(typeof(TNH_VoiceLineID)))
			{
				var linesofid = db.Lines.FindAll(line => line.ID == (TNH_VoiceLineID)vlid);
				if (linesofid.Count == 0)
				{
					var baselinesofid = __instance.VoiceDB.Lines.FindAll(line => line.ID == (TNH_VoiceLineID)vlid);
					db.Lines = db.Lines.Concat(baselinesofid).ToList();
				}
			}
			sw.Stop();
			__instance.VoiceDB = db;
			UnityEngine.Debug.Log(sw.ElapsedMilliseconds + "ms to load all voicelines!");

			/*for(int i=0; i < __instance.VoiceDB.Lines.Count; i++)
			{
				SavWav.Save("G:/exp/" + i + ".wav", __instance.VoiceDB.Lines[i].Clip_Standard);
			}*/
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