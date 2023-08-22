﻿using System.Linq;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	public class SoundtrackPatches {
		[HarmonyPatch(typeof(FVRFMODController), "SwitchTo")]
		[HarmonyPrefix]
		public static bool Patch_SwitchTo_PlaySoundtrackSongs(ref int musicIndex, ref float timeDelayStart, ref bool shouldStop, ref bool shouldDeadStop) {
			if (!SoundtrackAPI.SoundtrackEnabled)
				return true;
			int level = GM.TNH_Manager.m_level;
			//In the code, musicIndex 0 is the take theme and 1 is the hold theme.
			if (musicIndex == 1) {
				var holdMusic = SoundtrackAPI.GetAudioclipsForHold(level);
				if(holdMusic.Intro.Length > 0)
					TnHSoundtrack.Queue(holdMusic.Intro[Random.Range(0, holdMusic.Intro.Length)]); // Intro
				if (holdMusic.Phase.Count != 0) { // Using phases.
					for (int phase = 0; phase < holdMusic.Phase.Count; phase++) {
						TnHSoundtrack.Queue(holdMusic.Phase[phase][Random.Range(0, holdMusic.Phase.Count)]); // Phase
						TnHSoundtrack.Queue(holdMusic.PhaseTransition[phase][Random.Range(0, holdMusic.PhaseTransition.Count)]); // Phase Transition
					}
				}
				else { // Not using Phases. Continue as normal.
					TnHSoundtrack.Queue(holdMusic.Lo[Random.Range(0, holdMusic.Lo.Length)]); // Lo
					if(holdMusic.Transition.Length > 0)
						TnHSoundtrack.Queue(holdMusic.Transition[Random.Range(0, holdMusic.Transition.Length)]); // Transition
					if(holdMusic.MedHi.Length > 0)
						TnHSoundtrack.Queue(holdMusic.MedHi[Random.Range(0, holdMusic.MedHi.Length)]); // MedHi
				}
				if(holdMusic.End.Length > 0)
					TnHSoundtrack.Queue(holdMusic.End[Random.Range(0, holdMusic.End.Length)]); // End
				var take = SoundtrackAPI.GetAudioclipsForTake(level + 1);
				TnHSoundtrack.Queue(take.Track, "loop", take.Name, "take"); // Take song after the hold
				
				TnHSoundtrack.PlayNextSongInQueue(); // Plays next song, finishing Take and playing Intro (if exists, if not, Lo or Phases.)
			}
			return false;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool Patch_BeginPhase_HandlePhases() {
			if (!SoundtrackAPI.SoundtrackEnabled)
				return true;
			
			//If the next song is NOT a phase, just skip this whole bit.
			//This also handles phase overflow! if it overlfows, the next song would be End and itll just keep playing the highest phase until the actual end
			if (!TnHSoundtrack.SongQueue[0].type.Contains("phasetr"))
				return true;
			TnHSoundtrack.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool Patch_SetHoldWaveIntensity_TransitionToMedHi(ref int intensity) {
			if (!SoundtrackAPI.SoundtrackEnabled || intensity != 2)
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
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseTake_PlayEndAndTakeSong() {
			if (!SoundtrackAPI.SoundtrackEnabled || TnHSoundtrack.SongQueue.Count == 0)
				return true;
			// Just making sure it *skips* to End.
			// i stg if this null throws
			while (TnHSoundtrack.SongQueue[0].type != "end" && TnHSoundtrack.SongQueue[0].type != "take") {
				Debug.Log($"Skipping song {TnHSoundtrack.SongQueue[0].name} of type {TnHSoundtrack.SongQueue[0].type}");
				TnHSoundtrack.SongQueue.RemoveAt(0);
			}
			Debug.Log($"Playing end song.");
			TnHSoundtrack.PlayNextSongInQueue();
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
		[HarmonyPrefix]
		public static bool Patch_SetPhaseDead_PlayTake() {
			if (!SoundtrackAPI.SoundtrackEnabled)
				return true;
			Debug.Log($"Playing dead song.");
			var track = SoundtrackAPI.GetAudioclipsForTake(-1);
			TnHSoundtrack.SwitchSong(track.Track, track.Name, new []{"loop"} );
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool Patch_Start_AddTnHSoundtrack(ref TNH_Manager __instance) {
			if(SoundtrackAPI.SoundtrackEnabled)
				__instance.gameObject.AddComponent<TnHSoundtrack>();
			return true;
		}
	}
}