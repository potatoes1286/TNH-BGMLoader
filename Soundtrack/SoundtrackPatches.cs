using System.Collections.Generic;
using System.Linq;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	
	//Patches to H3VR code to allow soundtracks to play during TnH runs.
	public class SoundtrackPatches {

		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool Patch_BeginPhase_HandlePhases() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//If the next song is NOT a phase, just skip this whole bit.
			//This also handles phase overflow! if it overlfows, the next song would be End and itll just keep playing the highest phase until the actual end
			if (SoundtrackPlayer.SongQueue.Count > 0 && !SoundtrackPlayer.SongQueue[0].type.Contains("phasetr"))
				return true;
			SoundtrackPlayer.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool Patch_SetHoldWaveIntensity_TransitionToMedHi(ref int intensity) {
			if (!PluginMain.IsSoundtrack.Value || intensity != 2)
				return true;
			//If there's no MedHi queued, just skip it.
			if (SoundtrackPlayer.SongQueue.All(x => x.type != "medhi"))
				return true;
			// Just making sure it *skips* to Transition.
			// There's like, NO good reason this should be needed.
			// But i dont want to risk it.
			// i stg if this null throws
			while (SoundtrackPlayer.SongQueue[0].type != "transition" && SoundtrackPlayer.SongQueue[0].type != "medhi") {
				Debug.Log($"Skipping song {SoundtrackPlayer.SongQueue[0].name} of type {SoundtrackPlayer.SongQueue[0].type}");
				SoundtrackPlayer.SongQueue.RemoveAt(0);
			}
			SoundtrackPlayer.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "FailOut")]
		[HarmonyPrefix]
		public static bool Patch_FailOut_AddEndFailSong() {
			if (!PluginMain.IsSoundtrack.Value || SoundtrackPlayer.HoldMusic.EndFail.Length == 0)
				return true;
			//Replace end theme with the endfail theme
			for (int i = 0; i < SoundtrackPlayer.SongQueue.Count; i++)
				if(SoundtrackPlayer.SongQueue[i].type == "end")
					SoundtrackPlayer.SongQueue[i] = SoundtrackPlayer.HoldMusic.EndFail[Random.Range(0, SoundtrackPlayer.HoldMusic.EndFail.Length)];
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseTake_PlayEndAndTakeSong() {
			if (!PluginMain.IsSoundtrack.Value || SoundtrackPlayer.SongQueue.Count == 0)
				return true;
			// Just making sure it *skips* to End.
			// i stg if this null throws
			while (SoundtrackPlayer.SongQueue[0].type != "end" && SoundtrackPlayer.SongQueue[0].type != "take" && SoundtrackPlayer.SongQueue[0].type != "endfail") {
				Debug.Log($"Skipping song {SoundtrackPlayer.SongQueue[0].name} of type {SoundtrackPlayer.SongQueue[0].type}");
				SoundtrackPlayer.SongQueue.RemoveAt(0);
			}
			Debug.Log($"Playing end song.");
			SoundtrackPlayer.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseDead_PlayDeadSong() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-1);
			SoundtrackPlayer.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseCompleted_PlayWinSong() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			var track = SoundtrackAPI.GetAudioclipsForTake(-2);
			SoundtrackPlayer.SwitchSong(track.Track);
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void Patch_Start_AddTnHSoundtrack(ref TNH_Manager __instance) {
			if(PluginMain.IsSoundtrack.Value || SoundtrackAPI.IsMix)
				__instance.gameObject.AddComponent<TnHSoundtrackInterface>();
			// Turn off fmod.
			GM.TNH_Manager.FMODController.MasterBus.setMute(true);
			//Set hold music.

			SoundtrackPlayer.HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level);
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
			SoundtrackPlayer.failureSyncInfoReady = true;
			SoundtrackPlayer.timeIdentified = Time.time;
			//TickDownToFailure is fixed at 120. I think.
			SoundtrackPlayer.timeFail = Time.time + 120f + __instance.m_tickDownToIdentification;
		}
		
		//Implement OrbTouch funzies
		[HarmonyPatch(typeof(TNH_HoldPointSystemNode), "Start")]
		[HarmonyPrefix]
		public static bool Patch_Start_AddOrbTouchSong(ref TNH_HoldPointSystemNode __instance) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//Convert tracks to a list of audioclips
			var clips = new List<AudioClip>();
			if (SoundtrackPlayer.HoldMusic.OrbActivate.Length != 0) {
				foreach (var track in SoundtrackPlayer.HoldMusic.OrbActivate)
					clips.Add(track.clip);
				__instance.AUDEvent_HoldActivate.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (SoundtrackPlayer.HoldMusic.OrbHoldWave.Length != 0) {
				foreach (var track in SoundtrackPlayer.HoldMusic.OrbHoldWave)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_HoldWave.Clips = clips;
			}

			clips = new List<AudioClip>();
			if (SoundtrackPlayer.HoldMusic.OrbSuccess.Length != 0) {
				foreach (var track in SoundtrackPlayer.HoldMusic.OrbSuccess)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Success.Clips = clips;
			}
			
			clips = new List<AudioClip>();
			if (SoundtrackPlayer.HoldMusic.OrbFailure.Length != 0) {
				foreach (var track in SoundtrackPlayer.HoldMusic.OrbFailure)
					clips.Add(track.clip);
				__instance.HoldPoint.AUDEvent_Failure.Clips = clips;
			}
			
			
			return true;
		}
	}
}