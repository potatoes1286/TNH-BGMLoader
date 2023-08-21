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
			if (musicIndex == 1) {
				var holdMusic = SoundtrackAPI.GetAudioclipsForHold(level);
				TnHSoundtrack.SwitchSong(holdMusic.Intro, "Intro", false); // Forcefully set song to intro, which will end and go to
				TnHSoundtrack.Queue(holdMusic.Lo, true, true, "Lo"); // The Lo song, which will need to be manually skipped to
				TnHSoundtrack.Queue(holdMusic.Transition, true, false, "Transition"); // The transition song which ends and starts
				TnHSoundtrack.Queue(holdMusic.MedHi, true, true, "MedHi"); // The MedHi song, see Lo and then
				TnHSoundtrack.Queue(holdMusic.End, true, false, "End"); // The end song plays. Once that's over
				TnHSoundtrack.Queue(SoundtrackAPI.GetAudioclipsForTake(level + 1).Track, true, true, "Take"); // The Take song for the next level will play.
			}
			return false;
		}
		
		[HarmonyPatch(typeof(TNH_Manager), "SetHoldWaveIntensity")]
		[HarmonyPrefix]
		public static bool Patch_SetHoldWaveIntensity_TransitionToMedHi(ref int intensity) {
			if (!SoundtrackAPI.SoundtrackEnabled || intensity != 2)
				return true;
			// Just making sure it *skips* to Transition.
			// There's like, NO good reason this should be needed.
			// But i dont want to risk it.
			// i stg if this null throws
			while (TnHSoundtrack.SongQueue[0].name != "Transition") {
				Debug.Log($"Skipping song {TnHSoundtrack.SongQueue[0].name}");
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
			while (TnHSoundtrack.SongQueue[0].name != "End") {
				Debug.Log($"Skipping song {TnHSoundtrack.SongQueue[0].name}");
				TnHSoundtrack.SongQueue.RemoveAt(0);
			}
			Debug.Log($"Playing end song.");
			TnHSoundtrack.PlayNextSongInQueue();
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