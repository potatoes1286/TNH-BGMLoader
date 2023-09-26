using System.Collections.Generic;
using System.Linq;
using FistVR;
using UnityEngine;
using HarmonyLib;

namespace TNHBGLoader.Soundtrack {
	public class TnHSoundtrackInterface : SoundtrackPlayer {
		
		public static TrackSet HoldMusic;
		
		//Failure sync stuff.
		//A flip to let the switchsong know that the failruesync info is ready.
		public static bool  failureSyncInfoReady;
		public static float timeIdentified;
		public static float timeFail;
		
		
		public void Awake() {
			Initialize("tnh", SoundtrackAPI.GetCurrentSoundtrack, 1.5f, PluginMain.AnnouncerMusicVolume.Value / 4f);
			
			// Initialize holdmusic
			HoldMusic = SoundtrackAPI.GetSet("hold",Level + 1);
			
			PluginMain.DebugLog.LogInfo($"Level: {Level}");

			// If the hold music has its own take theme, play it
			if (HoldMusic.Tracks.Any(x => x.Type == "take"))
				QueueTake(HoldMusic);
			else //Otherwise, get a take theme.
				QueueTake(SoundtrackAPI.GetSet("take", 0));
		}

		public override void SwitchSong(Track newTrack, float timeOverride = -1f) {
			//Implement failure sync specific to TnH before passing off to generic SwitchSong
			bool failureSync = newTrack.Metadata.Any(x => x == "fs");
			float playHead = timeOverride;
			if (failureSync) {
				//The info is already there and waiting for us.
				if (failureSyncInfoReady) {
					float timeToFail = (timeFail - timeIdentified) - (Time.time - timeIdentified);
					//Ensure the song is long enough.
					if (timeToFail > SongLength) {
						PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]}:{newTrack.Name} is TOO SHORT! Song length: {SoundtrackPlayer.SongLength}, Time to Fail: {timeToFail}! Lengthen your song!");
						return;
					}
					playHead = (float)SongLength - timeToFail;
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
			HoldMusic = SoundtrackAPI.GetSet("hold",GM.TNH_Manager.m_level + 1);
			
			// If the hold music has its own take theme, play it
			if (HoldMusic.Tracks.Any(x => x.Type == "take"))
				QueueTake(HoldMusic);
			else //Otherwise, get a take theme.
				QueueTake(SoundtrackAPI.GetSet("take", GM.TNH_Manager.m_level + 1));
			
			Instance.PlayNextTrackInQueueOfType(new [] {"intro", "lo"});
				
			return false;
		}
		
		[HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
		[HarmonyPrefix]
		public static bool PlayPhaseTracks() {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			
			//If the next song is NOT a phase, just skip this whole bit.
			//This also handles phase overflow! if it overlfows, the next song would be End and itll just keep playing the highest phase until the actual end
			if (SongQueue.Count > 0 && !SongQueue[0].Type.Contains("phasetr"))
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
			if (SongQueue.All(x => x.Type != "medhi"))
				return true;
			// Just making sure it *skips* to Transition.
			// There's like, NO good reason this should be needed.
			// But i dont want to risk it.
			Instance.PlayNextTrackInQueueOfType(new [] {"transition", "medhi"});
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
		
		[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPostfix]
		public static void InitializeTnHSoundtrackInterface(ref TNH_Manager __instance) {
			if(PluginMain.IsSoundtrack.Value || SoundtrackAPI.IsMix)
				__instance.gameObject.AddComponent<TnHSoundtrackInterface>();
			// Turn off fmod.
			GM.TNH_Manager.FMODController.MasterBus.setMute(true);
			//Set hold music.
			
			//HoldMusic = SoundtrackAPI.GetSet("hold", GM.TNH_Manager.m_level);
			PluginMain.DebugLog.LogInfo($"IsMix: {SoundtrackAPI.IsMix.ToString()}, CurSoundtrack: {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex].Guid}, IsSOundtrack: {PluginMain.IsSoundtrack.Value}");
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