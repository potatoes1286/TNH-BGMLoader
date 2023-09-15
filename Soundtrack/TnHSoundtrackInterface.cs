using System.Collections.Generic;
using System.Linq;
using FistVR;
using UnityEngine;
using HarmonyLib;

namespace TNHBGLoader.Soundtrack {
	public class TnHSoundtrackInterface : SoundtrackPlayer {
		
		public static HoldData? HoldMusic;
		
		//Failure sync stuff.
		//A flip to let the switchsong know that the failruesync info is ready.
		public static bool  failureSyncInfoReady;
		public static float timeIdentified;
		public static float timeFail;
		
		
		public void Awake() {
			Initialize("tnh", SoundtrackAPI.GetCurrentSoundtrack, 1.5f, PluginMain.AnnouncerMusicVolume.Value / 4f);
			
			HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level + 1);
			
			Track take;
			if (HoldMusic.Take.Length == 0)
				take = SoundtrackAPI.GetAudioclipsForTake(0).Track;
			else
				take = HoldMusic.Take[Random.Range(0, HoldMusic.Take.Length)];
			SwitchSong(take); //start playing take theme
		}

		public override void SwitchSong(Track newSong, float timeOverride = -1f) {
			//Implement failure sync specific to TnH before passing off to generic SwitchSong
			bool failureSync = newSong.metadata.Any(x => x == "fs");
			float playHead = timeOverride;
			if (failureSync) {
				//The info is already there and waiting for us.
				if (failureSyncInfoReady) {
					float timeToFail = (timeFail - timeIdentified) - (Time.time - timeIdentified);
					//Ensure the song is long enough.
					if (timeToFail > SongLength) {
						PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]}:{newSong.name} is TOO SHORT! Song length: {SoundtrackPlayer.SongLength}, Time to Fail: {timeToFail}! Lengthen your song!");
						return;
					}
					playHead = (float)SongLength - timeToFail;
					failureSyncInfoReady = false;
				}
				//It hasn't been identified yet. Just fucking throw.
				else {
					PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]}:{newSong.name} DID NOT have enough time to get info about how long the hold is! (FailureSync). Please lengthen your transition or intro to give more buffer time for the info to load! It should be AT LEAST 5 seconds.");
				}
			}
			//Pass on to generic SwitchSong
			base.SwitchSong(newSong, playHead);
		}
		
		[HarmonyPatch(typeof(FVRFMODController), "SwitchTo")]
		[HarmonyPrefix]
		public static bool QueueHoldAndTakeTracks(ref int musicIndex, ref float timeDelayStart, ref bool shouldStop, ref bool shouldDeadStop) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			//In the code, musicIndex 0 is the take theme and 1 is the hold theme.
			if (musicIndex == 1) {
				//HoldMusic should be initialized by now in Patch_Start_AddOrbTouchSong
				if(HoldMusic == null)
					PluginMain.DebugLog.LogError("Failed to initialize soundtrack hold data! What did you do!?");
				if(HoldMusic.Intro.Length > 0)
					Instance.Queue(HoldMusic.Intro[Random.Range(0, HoldMusic.Intro.Length)]); // Intro
				if (HoldMusic.Phase.Count != 0) { // Using phases.
					PluginMain.DebugLog.LogInfo("Using Phases.");
					for (int phase = 0; phase < HoldMusic.Phase.Count; phase++) {
						Instance.Queue(HoldMusic.Phase[phase][Random.Range(0, HoldMusic.Phase[phase].Count)]); // Phase
						
						if(phase < HoldMusic.PhaseTransition.Count && HoldMusic.PhaseTransition[phase].Count > 0)
							Instance.Queue(HoldMusic.PhaseTransition[phase][Random.Range(0, HoldMusic.PhaseTransition[phase].Count)]); // Phase Transition
					}
				}
				else { // Not using Phases. Continue as normal.
					Instance.Queue(HoldMusic.Lo[Random.Range(0, HoldMusic.Lo.Length)]); // Lo
					if(HoldMusic.Transition.Length > 0)
						Instance.Queue(HoldMusic.Transition[Random.Range(0, HoldMusic.Transition.Length)]); // Transition
					if(HoldMusic.MedHi.Length > 0)
						Instance.Queue(HoldMusic.MedHi[Random.Range(0, HoldMusic.MedHi.Length)]); // MedHi
				}
				if(HoldMusic.End.Length > 0)
					Instance.Queue(HoldMusic.End[Random.Range(0, HoldMusic.End.Length)]); // End

				/*if (SoundtrackAPI.IsMix && SoundtrackAPI.Soundtracks.Count != 1) {
					var curSt = SoundtrackAPI.SelectedSoundtrackIndex;
					int newSt = curSt;
					for (int i = 0; i < 10; i++) {
						newSt = UnityEngine.Random.Range(0, SoundtrackAPI.Soundtracks.Count);
						if (newSt != curSt)
							break;
					}
					SoundtrackAPI.SelectedSoundtrackIndex = newSt;
					PluginMain.DebugLog.LogDebug($"IsMix: {SoundtrackAPI.IsMix}, Switched from old soundtrack {SoundtrackAPI.Soundtracks[curSt].Guid} to {SoundtrackAPI.Soundtracks[newSt].Guid}");
				}*/

				Track take;
				HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level + 1);
				if (HoldMusic.Take.Length == 0) {
					Debug.Log($"Getting audioclips for take {GM.TNH_Manager.m_level + 1}.");
					take = SoundtrackAPI.GetAudioclipsForTake(GM.TNH_Manager.m_level + 1).Track;
				}
				else {
					take = HoldMusic.Take[Random.Range(0, HoldMusic.Take.Length)];
				}

				Instance.Queue(take);
				Instance.PlayNextSongInQueue(); // Plays next song, finishing Take and playing Intro (if exists, if not, Lo or Phases.)
			}
			return false;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool PlayPhaseTracks() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//If the next song is NOT a phase, just skip this whole bit.
			//This also handles phase overflow! if it overlfows, the next song would be End and itll just keep playing the highest phase until the actual end
			if (SongQueue.Count > 0 && !SongQueue[0].type.Contains("phasetr"))
				return true;
			Instance.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool TransitionToMedHiTrack(ref int intensity) {
			if (!PluginMain.IsSoundtrack.Value || intensity != 2)
				return true;
			//If there's no MedHi queued, just skip it.
			if (SongQueue.All(x => x.type != "medhi"))
				return true;
			// Just making sure it *skips* to Transition.
			// There's like, NO good reason this should be needed.
			// But i dont want to risk it.
			// i stg if this null throws
			while (SongQueue[0].type != "transition" && SongQueue[0].type != "medhi") {
				Debug.Log($"Skipping song {SongQueue[0].name} of type {SongQueue[0].type}");
				SongQueue.RemoveAt(0);
			}
			Instance.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "FailOut")]
		[HarmonyPrefix]
		public static bool QueueFailTrack() {
			if (!PluginMain.IsSoundtrack.Value || HoldMusic.EndFail.Length == 0)
				return true;
			//Replace end theme with the endfail theme
			for (int i = 0; i < SongQueue.Count; i++)
				if(SongQueue[i].type == "end")
					SongQueue[i] = HoldMusic.EndFail[Random.Range(0, HoldMusic.EndFail.Length)];
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
		[HarmonyPrefix]
		public static bool SkipToEndTrack() {
			if (!PluginMain.IsSoundtrack.Value || SongQueue.Count == 0)
				return true;
			// Just making sure it *skips* to End.
			// i stg if this null throws
			while (SongQueue[0].type != "end" && SongQueue[0].type != "take" && SongQueue[0].type != "endfail") {
				Debug.Log($"Skipping song {SongQueue[0].name} of type {SongQueue[0].type}");
				SongQueue.RemoveAt(0);
			}
			Debug.Log($"Playing end song.");
			Instance.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
		[HarmonyPrefix]
		public static bool PlayDeadTrack() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-1);
			Instance.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
		[HarmonyPrefix]
		public static bool PlayWinTrack() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-2);
			Instance.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void InitializeTnHSoundtrackInterface(ref TNH_Manager __instance) {
			if(PluginMain.IsSoundtrack.Value || SoundtrackAPI.IsMix)
				__instance.gameObject.AddComponent<TnHSoundtrackInterface>();
			// Turn off fmod.
			GM.TNH_Manager.FMODController.MasterBus.setMute(true);
			//Set hold music.

			HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level);
			Debug.Log($"IsMix: {SoundtrackAPI.IsMix.ToString()}, CurSoundtrack: {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex].Guid}, IsSOundtrack: {PluginMain.IsSoundtrack.Value}");
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
			//TickDownToFailure is fixed at 120. I think.
			timeFail = Time.time + 120f + __instance.m_tickDownToIdentification;
		}
		
		//Implement OrbTouch funzies
		[HarmonyPatch(typeof(TNH_HoldPointSystemNode), "Start")]
		[HarmonyPrefix]
		public static bool SetOrbTouchTracks(ref TNH_HoldPointSystemNode __instance) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//Convert tracks to a list of audioclips
			var clips = new List<AudioClip>();
			if (HoldMusic.OrbActivate.Length != 0) {
				foreach (var track in HoldMusic.OrbActivate)
					clips.Add(track.clip);
				__instance.AUDEvent_HoldActivate.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (HoldMusic.OrbHoldWave.Length != 0) {
				foreach (var track in HoldMusic.OrbHoldWave)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_HoldWave.Clips = clips;
			}

			clips = new List<AudioClip>();
			if (HoldMusic.OrbSuccess.Length != 0) {
				foreach (var track in HoldMusic.OrbSuccess)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Success.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (HoldMusic.OrbFailure.Length != 0) {
				foreach (var track in HoldMusic.OrbFailure)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Failure.Clips = clips;
			}
			
			return true;
		}
	}
}