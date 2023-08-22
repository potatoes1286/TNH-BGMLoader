using System.Linq;
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
				if(holdMusic.Intro.Length > 0)
					TnHSoundtrack.Queue(holdMusic.Intro[Random.Range(0, holdMusic.Intro.Length)]); // Forcefully set song to intro, which will end and go to
				TnHSoundtrack.Queue(holdMusic.Lo[Random.Range(0, holdMusic.Intro.Length)]); // The Lo song, which will need to be manually skipped to
				if(holdMusic.Transition.Length > 0)
					TnHSoundtrack.Queue(holdMusic.Transition[Random.Range(0, holdMusic.Intro.Length)]); // The transition song which ends and starts
				if(holdMusic.MedHi.Length > 0)
					TnHSoundtrack.Queue(holdMusic.MedHi[Random.Range(0, holdMusic.Intro.Length)]); // The MedHi song, see Lo and then
				if(holdMusic.End.Length > 0)
					TnHSoundtrack.Queue(holdMusic.End[Random.Range(0, holdMusic.Intro.Length)]); // The end song plays. Once that's over
				var take = SoundtrackAPI.GetAudioclipsForTake(level + 1);
				TnHSoundtrack.Queue(take.Track, "loop", take.Name, "take"); // The Take song for the next level will play.
				
				TnHSoundtrack.PlayNextSongInQueue();
			}
			return false;
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
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool Patch_Start_AddTnHSoundtrack(ref TNH_Manager __instance) {
			if(SoundtrackAPI.SoundtrackEnabled)
				__instance.gameObject.AddComponent<TnHSoundtrack>();
			return true;
		}
	}
}