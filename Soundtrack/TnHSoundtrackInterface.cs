using System;
using System.Collections.Generic;
using System.Linq;
using FistVR;
using UnityEngine;
using HarmonyLib;
using Sodalite.Utilities;
using Random = UnityEngine.Random;

namespace TNHBGLoader.Soundtrack {
	public class TnHSoundtrackInterface : SoundtrackPlayer {

		public static TNH_Manager Manager;
		
		public static TrackSet HoldMusic;
		public static TrackSet TakeMusic;
		
		//Failure sync stuff.
		//A flip to let the switchsong know that the failruesync info is ready.
		public static bool  failureSyncInfoReady;
		public static float timeIdentified;
		public static float timeFail;
		public static bool  isInstitutionMode      = false;
		public static int   currentInstitutionArea = 0;
		
		// Alert info
		public static bool  currentlyInAlert = false;
		public static Track storedTakeTrack;
		public static float storedTakeTrackPlayhead;
		public static bool  changedAreaMidFight;

		public static int Level;
		
		
		public void Awake() {
			Initialize("tnh", SoundtrackAPI.GetCurrentSoundtrack, 1.5f, PluginMain.AnnouncerMusicVolume.Value / 4f);
			
			ClearQueue();
			Level = 0;
		}

		public override void SwitchSong(Track newTrack, float timeOverride = -1f) {
			//Implement failure sync specific to TnH before passing off to generic SwitchSong
			bool failureSync = newTrack.Metadata.Any(x => x == "fs");
			float playHead = timeOverride % newTrack.Clip.length;
			if (failureSync) {
				//The info is already there and waiting for us.
				if (failureSyncInfoReady) {
					float timeToFail = (timeFail - timeIdentified) - (Time.time - timeIdentified);
					//Ensure the song is long enough.
					if (timeToFail > newTrack.Clip.length) {
						PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]}:{newTrack.Name} is TOO SHORT! Song length: {newTrack.Clip.length}, Time to Fail: {timeToFail}! Lengthen your song!");
						return;
					}
					playHead = newTrack.Clip.length - timeToFail;
					failureSyncInfoReady = false;
				}
				//It hasn't been identified yet. Just fucking throw.
				else {
					PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]}:{newTrack.Name} DID NOT have enough time to get info about how long the hold is! (FailureSync). Please lengthen your transition or intro to give more buffer time for the info to load! It should be AT LEAST 5 seconds.");
				}
			}
			//Pass on to generic SwitchSong
			base.SwitchSong(newTrack, playHead);
		}

		// Areas:
		// 0 - Central - center
		// 1 - Govt - north
		// 2 - Indus - south
		// 3 - Temple - west
		// 4 - Power - east
		public static readonly string[] AreaFromInt = { "center", "north", "south", "west", "east" };
		
		
		
		// handles institution changing areas
		// also runs at start of run
		[HarmonyPatch(typeof(FVRFMODController), "SetIntParameterByIndex")]
		[HarmonyPrefix]
		public static bool InstitutionChangeRegion(ref string s, ref float f) {
			if (!PluginMain.IsSoundtrack.Value) 
				return true;
			if(isInstitutionMode && s == "TAH2 Area") {
				int newArea = (int)f;
				if (currentInstitutionArea != newArea) {
					if(!currentlyInAlert) // Don't do this if we're in the middle of a fight!
						ChangeArea(newArea);
					if (currentlyInAlert)
						changedAreaMidFight = true;
					PluginMain.DebugLog.LogInfo($"Changing to area {newArea}");
					currentInstitutionArea = newArea;
				}
			}
			return true;
		}

		public void FixedUpdate() {
			// alert system
			if (PluginMain.IsSoundtrack.Value) {
				bool newAlert = false;
				if(isInstitutionMode)
					newAlert = (int)Manager.m_takeMusicIntensity >= 1;
				else
					newAlert = (int)Manager.m_takeMusicIntensity == 2;
				if (currentlyInAlert != newAlert) {
					if (newAlert) { //entering alert mode
						var alerts = HoldMusic.Tracks.Where(x => x.Type == "alert").ToArray();
						if(!alerts.Any())
							alerts = TakeMusic.Tracks.Where(x => x.Type == "alert").ToArray();
						if (alerts.Any()) { // there are alerts in the soundtrack
							storedTakeTrack = CurrentTrack;
							storedTakeTrackPlayhead = Instance.GetCurrentAudioSource.time;
							Instance.SwitchSong(alerts.GetRandom());
						}
					}
					else { //exiting alert mode
						// Handle if changed area mid fight
						if (changedAreaMidFight) {
							changedAreaMidFight = false;
							ChangeArea(currentInstitutionArea);
						} // Handle if did not.
						else {
							float alertPlayhead = Instance.GetCurrentAudioSource.time;
							float time = storedTakeTrackPlayhead + alertPlayhead;
							if (storedTakeTrack.Metadata.Contains("restart"))
								time = 0;
							if (storedTakeTrack.Metadata.Contains("return"))
								time = storedTakeTrackPlayhead;
							Instance.SwitchSong(storedTakeTrack, time);
						}
					}
					currentlyInAlert = newAlert;
				}
			}
		}

		public static void ChangeArea(int newArea) {
			Instance.ClearQueue();
			
			HoldMusic = SoundtrackAPI.GetSetWithMetadata("hold", Level, new []{ AreaFromInt[newArea] });
			
			// If the hold music has its own take theme, play it
			if (HoldMusic.Tracks.Any(x => x.Type == "take"))
				QueueTake(HoldMusic);
			else //Otherwise, get a take theme.
				QueueTake(SoundtrackAPI.GetSetWithMetadata("take", Level, new []{ AreaFromInt[newArea] }));
			
			Instance.PlayNextSongInQueue();
		}
		
		// Occurs at the start of a hold
		// Queues in all of the hold songs, and the next-take songs, then starts playing the hold songs
		[HarmonyPatch(typeof(FVRFMODController), "SwitchTo")]
		[HarmonyPrefix]
		public static bool QueueHoldAndTakeTracks(ref int  musicIndex, ref float timeDelayStart, ref bool shouldStop, ref bool shouldDeadStop) {
			if (!PluginMain.IsSoundtrack.Value) 
				return true;
			if (musicIndex != 1) // musicIndex 1 is hold, 0 is take
				return false;
			//Queue intro, if exists
			Instance.QueueRandomOfType(HoldMusic, "intro", false);

			var isUsingPhases = HoldMusic.Tracks.Any(x => x.Type.Contains("phase"));
			if (!isUsingPhases) {
				//Queue Lo
				Instance.QueueRandomOfType(HoldMusic, "lo", true);
				
				//Queue transition, if exists
				Instance.QueueRandomOfType(HoldMusic, "transition", false);
				
				//Queue MedHi, if exists
				Instance.QueueRandomOfType(HoldMusic, "medhi", false);
			}
			else { //Is using phases
				//Queue a phase track for each phase, 0, 1, 2... until runs out. Or hits limit of 20. You should NOT be hitting 20 phases.
				int phaseNo = 0;
				while (HoldMusic.Tracks.Any(x => x.Type == "phase" + phaseNo) && phaseNo < 20) {
					//Queue phase track
					Instance.QueueRandomOfType(HoldMusic, "phase" + phaseNo);
					//Queue phase transition track, if exists
					Instance.QueueRandomOfType(HoldMusic, "phasetr" + phaseNo, false);
					phaseNo++;
				}
			}
			//Queue end, if exists
			Instance.QueueRandomOfType(HoldMusic, "end", false);

			Level++;
			PluginMain.DebugLog.LogInfo($"Level: {Level}");
			
			// Initialize holdmusic for next hold/take
			if(!isInstitutionMode)
				HoldMusic = SoundtrackAPI.GetSet("hold",Level);
			else
				HoldMusic = SoundtrackAPI.GetSetWithMetadata("hold",Level, new[] { AreaFromInt[currentInstitutionArea] });
			
			// If the hold music has its own take theme, play it
			if (HoldMusic.Tracks.Any(x => x.Type == "take"))
				QueueTake(HoldMusic);
			else { //Otherwise, get a take theme.
				TakeMusic = SoundtrackAPI.GetSet("take", Level);
				QueueTake(TakeMusic);
			}

			Instance.PlayNextTrackInQueueOfType(new[] { "intro", "lo", "phase0"});

			return false;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool PlayPhaseTracks() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			if (SongQueue.Count == 0 || CurrentTrack.Type == "intro") // Don't cut off the fuckin intro
				return true;
			if(SongQueue[0].Type.Contains("phasetr") || SongQueue[0].Type.Contains("phase"))
				Instance.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginAnalyzing")]
		[HarmonyPrefix]
		public static bool PlayMedHiTrack(TNH_HoldPoint __instance) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			// if there's a medhi and no transition, only play the medhi at the begin analyzing stage where the transition
			// would normally end
			// otherwise if there is no transition, play medhi only at beginanalyzing
			if (SongQueue.Any(x => x.Type == "transition") // make sure there isn't a transition queued, otherwise don't do this
			 || CurrentTrack.Type == "transition" // make sure transition isnt playing so we dont prematurely cut transition off
			 || Intensity != 2) // make sure it's actually time to play transition/medhi
				return true;
			if (SongQueue.Any(x => x.Type == "medhi"))
				Instance.PlayNextTrackInQueueOfType(new [] {"medhi"});
			return true;
		}
	
		// hack
		public static int Intensity = 1;
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool PlayTransitionTrack(ref int intensity) {
			Intensity = intensity;
			if (!PluginMain.IsSoundtrack.Value || intensity != 2)
				return true;
			// There shouldnt even be a transition if theres no medhi
			if (SongQueue.All(x => x.Type != "medhi"))
				return true;
			// If there's a transition, skip to it.
			if (SongQueue.Any(x => x.Type == "transition"))
				Instance.PlayNextTrackInQueueOfType(new [] {"transition"});
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "FailOut")]
		[HarmonyPrefix]
		public static bool QueueFailTrack() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var endFailTracks = HoldMusic.Tracks.Where(x => x.Type == "endfail").ToArray();
			if (endFailTracks.Length == 0)
				return true;
			//Replace end theme with the endfail theme
			for (int i = 0; i < SongQueue.Count; i++)
				if(SongQueue[i].Type == "end")
					SongQueue[i] = endFailTracks[Random.Range(0, endFailTracks.Length)];
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
		[HarmonyPrefix]
		public static bool SkipToEndTrack() {
			if (!PluginMain.IsSoundtrack.Value || SongQueue.Count == 0)
				return true;
			// Just making sure it *skips* to End.
			Instance.PlayNextTrackInQueueOfType(new [] {"end", "take", "takeintro", "endfail"});
			return true;
		}

		public static void QueueTake(int situation) {
			var set = SoundtrackAPI.GetSet("take", situation);
			QueueTake(set);
		}
		
		public static void QueueTake(TrackSet set) {
			Instance.QueueRandomOfType(set, "takeintro", false);
			Instance.QueueRandomOfType(set, "take");
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
		[HarmonyPrefix]
		public static bool PlayDeadTrack() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			Instance.ClearQueue();
			QueueTake(-1);
			Instance.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
		[HarmonyPrefix]
		public static bool PlayWinTrack() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			Instance.ClearQueue();
			QueueTake(-2);
			Instance.PlayNextSongInQueue();
			return true;
		}
		
		// Initialize everything
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void InitializeTnHSoundtrackInterface(ref TNH_Manager __instance) {
			Manager = __instance;
			isInstitutionMode = __instance.UsesInstitutionMusic;

			if(PluginMain.IsSoundtrack.Value || SoundtrackAPI.IsMix)
				__instance.gameObject.AddComponent<TnHSoundtrackInterface>();
			// Turn off fmod.
			GM.TNH_Manager.FMODController.MasterBus.setMute(true);
			//Set hold music.
			
			PluginMain.DebugLog.LogInfo($"IsMix: {SoundtrackAPI.IsMix.ToString()}, CurSoundtrack: {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex].Guid}, IsSOundtrack: {PluginMain.IsSoundtrack.Value}");
			
			// Initialize holdmusic
			PluginMain.DebugLog.LogInfo($"Level: {Level}");

			// if it is institution mode, this will be initiated on InstitutionChangeRegion
			if (!isInstitutionMode) {
				HoldMusic = SoundtrackAPI.GetSet("hold", Level);

				// If the hold music has its own take theme, play it
				if (HoldMusic.Tracks.Any(x => x.Type == "take"))
					QueueTake(HoldMusic);
				else { //Otherwise, get a take theme.
					TakeMusic = SoundtrackAPI.GetSet("take", Level);
					QueueTake(TakeMusic);
				}
			}
		}
		
		
		
		
		/*[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void Patch_Start_PrintPhaseDeets(ref TNH_Manager __instance) {
			//Print out the length to failure for Failure Sync.
			foreach (var level in __instance.m_curProgression.Levels)
				for(int i = 0; i < level.HoldChallenge.Phases.Count; i++)
					PluginMain.DebugLog.LogInfo($"Level: {level}, phase: {i}, length: {level.HoldChallenge.Phases[i].ScanTime * 0.8 + 120} - {level.HoldChallenge.Phases[i].ScanTime * 1.2 + 120}");
		}*/

		
		//Handle Failure Sync
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginAnalyzing")]
		[HarmonyPostfix]
		public static void GatherFailureSyncInfo(ref TNH_HoldPoint __instance) {
			failureSyncInfoReady = true;
			timeIdentified = Time.time;
			//TickDownToFailure is fixed at 120. I think. 3 secs is for buffer.
			timeFail = Time.time + 120f + __instance.m_tickDownToIdentification + 3f;
		}
		
		//Implement OrbTouch funzies
		[HarmonyPatch(typeof(TNH_HoldPointSystemNode), "Start")]
		[HarmonyPrefix]
		public static bool SetOrbTouchTracks(ref TNH_HoldPointSystemNode __instance) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//Convert tracks to a list of audioclips
			var clips = new List<AudioClip>();
			var tracks = HoldMusic.Tracks.Where(x => x.Type == "orbactivate").ToArray();
			if (tracks.Length != 0) {
				foreach (var track in tracks)
					clips.Add(track.Clip);
				__instance.AUDEvent_HoldActivate.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			tracks = HoldMusic.Tracks.Where(x => x.Type == "orbwave").ToArray();
			if (tracks.Length != 0) {
				foreach (var track in tracks)
					clips.Add(track.Clip);
				__instance.HoldPoint.AUDEvent_HoldWave.Clips = clips;
			}

			clips = new List<AudioClip>();
			tracks = HoldMusic.Tracks.Where(x => x.Type == "orbsuccess").ToArray();
			if (tracks.Length != 0) {
				foreach (var track in tracks)
					clips.Add(track.Clip);
				__instance.HoldPoint.AUDEvent_Success.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			tracks = HoldMusic.Tracks.Where(x => x.Type == "orbfailure").ToArray();
			if (tracks.Length != 0) {
				foreach (var track in tracks)
					clips.Add(track.Clip);
				__instance.HoldPoint.AUDEvent_Failure.Clips = clips;
			}
			
			return true;
		}
	}
}