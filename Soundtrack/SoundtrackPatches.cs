using System.Collections.Generic;
using System.Linq;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	
	//Patches to H3VR code to allow soundtracks to play during TnH runs.
	public class SoundtrackPatches {
		[HarmonyPatch(typeof(FVRFMODController), "SwitchTo")]
		[HarmonyPrefix]
		public static bool Patch_SwitchTo_PlaySoundtrackSongs(ref int musicIndex, ref float timeDelayStart, ref bool shouldStop, ref bool shouldDeadStop) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			//In the code, musicIndex 0 is the take theme and 1 is the hold theme.
			if (musicIndex == 1) {
				//HoldMusic should be initialized by now in Patch_Start_AddOrbTouchSong
				if(TnHSoundtrack.HoldMusic == null)
					PluginMain.DebugLog.LogError("Failed to initialize soundtrack hold data! What did you do!?");
				if(TnHSoundtrack.HoldMusic.Intro.Length > 0)
					TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.Intro[Random.Range(0, TnHSoundtrack.HoldMusic.Intro.Length)]); // Intro
				if (TnHSoundtrack.HoldMusic.Phase.Count != 0) { // Using phases.
					PluginMain.DebugLog.LogInfo("Using Phases.");
					for (int phase = 0; phase < TnHSoundtrack.HoldMusic.Phase.Count; phase++) {
						TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.Phase[phase][Random.Range(0, TnHSoundtrack.HoldMusic.Phase[phase].Count)]); // Phase
						
						if(phase < TnHSoundtrack.HoldMusic.PhaseTransition.Count && TnHSoundtrack.HoldMusic.PhaseTransition[phase].Count > 0)
							TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.PhaseTransition[phase][Random.Range(0, TnHSoundtrack.HoldMusic.PhaseTransition[phase].Count)]); // Phase Transition
					}
				}
				else { // Not using Phases. Continue as normal.
					TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.Lo[Random.Range(0, TnHSoundtrack.HoldMusic.Lo.Length)]); // Lo
					if(TnHSoundtrack.HoldMusic.Transition.Length > 0)
						TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.Transition[Random.Range(0, TnHSoundtrack.HoldMusic.Transition.Length)]); // Transition
					if(TnHSoundtrack.HoldMusic.MedHi.Length > 0)
						TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.MedHi[Random.Range(0, TnHSoundtrack.HoldMusic.MedHi.Length)]); // MedHi
				}
				if(TnHSoundtrack.HoldMusic.End.Length > 0)
					TnHSoundtrack.Queue(TnHSoundtrack.HoldMusic.End[Random.Range(0, TnHSoundtrack.HoldMusic.End.Length)]); // End

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
				TnHSoundtrack.HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level + 1);
				if (TnHSoundtrack.HoldMusic.Take.Length == 0) {
					Debug.Log($"Getting audioclips for take {GM.TNH_Manager.m_level + 1}.");
					take = SoundtrackAPI.GetAudioclipsForTake(GM.TNH_Manager.m_level + 1).Track;
				}
				else {
					take = TnHSoundtrack.HoldMusic.Take[Random.Range(0, TnHSoundtrack.HoldMusic.Take.Length)];
				}

				TnHSoundtrack.Queue(take);
				TnHSoundtrack.PlayNextSongInQueue(); // Plays next song, finishing Take and playing Intro (if exists, if not, Lo or Phases.)
			}
			return false;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool Patch_BeginPhase_HandlePhases() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//If the next song is NOT a phase, just skip this whole bit.
			//This also handles phase overflow! if it overlfows, the next song would be End and itll just keep playing the highest phase until the actual end
			if (TnHSoundtrack.SongQueue.Count > 0 && !TnHSoundtrack.SongQueue[0].type.Contains("phasetr"))
				return true;
			TnHSoundtrack.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool Patch_SetHoldWaveIntensity_TransitionToMedHi(ref int intensity) {
			if (!PluginMain.IsSoundtrack.Value || intensity != 2)
				return true;
			//If there's no MedHi queued, just skip it.
			if (TnHSoundtrack.SongQueue.All(x => x.type != "medhi"))
				return true;
			// Just making sure it *skips* to Transition.
			// There's like, NO good reason this should be needed.
			// But i dont want to risk it.
			// i stg if this null throws
			while (TnHSoundtrack.SongQueue[0].type != "transition" && TnHSoundtrack.SongQueue[0].type != "medhi") {
				Debug.Log($"Skipping song {TnHSoundtrack.SongQueue[0].name} of type {TnHSoundtrack.SongQueue[0].type}");
				TnHSoundtrack.SongQueue.RemoveAt(0);
			}
			TnHSoundtrack.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "FailOut")]
		[HarmonyPrefix]
		public static bool Patch_FailOut_AddEndFailSong() {
			if (!PluginMain.IsSoundtrack.Value || TnHSoundtrack.HoldMusic.EndFail.Length == 0)
				return true;
			//Replace end theme with the endfail theme
			for (int i = 0; i < TnHSoundtrack.SongQueue.Count; i++)
				if(TnHSoundtrack.SongQueue[i].type == "end")
					TnHSoundtrack.SongQueue[i] = TnHSoundtrack.HoldMusic.EndFail[Random.Range(0, TnHSoundtrack.HoldMusic.EndFail.Length)];
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseTake_PlayEndAndTakeSong() {
			if (!PluginMain.IsSoundtrack.Value || TnHSoundtrack.SongQueue.Count == 0)
				return true;
			// Just making sure it *skips* to End.
			// i stg if this null throws
			while (TnHSoundtrack.SongQueue[0].type != "end" && TnHSoundtrack.SongQueue[0].type != "take" && TnHSoundtrack.SongQueue[0].type != "endfail") {
				Debug.Log($"Skipping song {TnHSoundtrack.SongQueue[0].name} of type {TnHSoundtrack.SongQueue[0].type}");
				TnHSoundtrack.SongQueue.RemoveAt(0);
			}
			Debug.Log($"Playing end song.");
			TnHSoundtrack.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseDead_PlayDeadSong() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-1);
			TnHSoundtrack.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseCompleted_PlayWinSong() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-2);
			TnHSoundtrack.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void Patch_Start_AddTnHSoundtrack(ref TNH_Manager __instance) {
			if(PluginMain.IsSoundtrack.Value || SoundtrackAPI.IsMix)
				__instance.gameObject.AddComponent<TnHSoundtrack>();
			// Turn off fmod.
			GM.TNH_Manager.FMODController.MasterBus.setMute(true);
			//Set hold music.

			TnHSoundtrack.HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level);
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
		public static void Patch_BeginAnalyzing_HandleFailureSync(ref TNH_HoldPoint __instance) {
			TnHSoundtrack.failureSyncInfoReady = true;
			TnHSoundtrack.timeIdentified = Time.time;
			//TickDownToFailure is fixed at 120. I think.
			TnHSoundtrack.timeFail = Time.time + 120f + __instance.m_tickDownToIdentification;
		}
		
		//Implement OrbTouch funzies
		[HarmonyPatch(typeof(TNH_HoldPointSystemNode), "Start")]
		[HarmonyPrefix]
		public static bool Patch_Start_AddOrbTouchSong(ref TNH_HoldPointSystemNode __instance) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//Convert tracks to a list of audioclips
			var clips = new List<AudioClip>();
			if (TnHSoundtrack.HoldMusic.OrbActivate.Length != 0) {
				foreach (var track in TnHSoundtrack.HoldMusic.OrbActivate)
					clips.Add(track.clip);
				__instance.AUDEvent_HoldActivate.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (TnHSoundtrack.HoldMusic.OrbHoldWave.Length != 0) {
				foreach (var track in TnHSoundtrack.HoldMusic.OrbHoldWave)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_HoldWave.Clips = clips;
			}

			clips = new List<AudioClip>();
			if (TnHSoundtrack.HoldMusic.OrbSuccess.Length != 0) {
				foreach (var track in TnHSoundtrack.HoldMusic.OrbSuccess)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Success.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (TnHSoundtrack.HoldMusic.OrbFailure.Length != 0) {
				foreach (var track in TnHSoundtrack.HoldMusic.OrbFailure)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Failure.Clips = clips;
			}
			
			
			return true;
		}
	}
}