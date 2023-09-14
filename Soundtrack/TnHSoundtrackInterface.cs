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
			Initialize(SoundtrackAPI.GetCurrentSoundtrack, 1.5f, PluginMain.AnnouncerMusicVolume.Value / 4f);
			
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
		public static bool Patch_SwitchTo_PlaySoundtrackSongs(ref int musicIndex, ref float timeDelayStart, ref bool shouldStop, ref bool shouldDeadStop) {
			if (!PluginMain.IsSoundtrack.Value)
				return true;
			//In the code, musicIndex 0 is the take theme and 1 is the hold theme.
			if (musicIndex == 1) {
				//HoldMusic should be initialized by now in Patch_Start_AddOrbTouchSong
				if(HoldMusic == null)
					PluginMain.DebugLog.LogError("Failed to initialize soundtrack hold data! What did you do!?");
				if(SoundtrackPlayer.HoldMusic.Intro.Length > 0)
					SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.Intro[Random.Range(0, SoundtrackPlayer.HoldMusic.Intro.Length)]); // Intro
				if (SoundtrackPlayer.HoldMusic.Phase.Count != 0) { // Using phases.
					PluginMain.DebugLog.LogInfo("Using Phases.");
					for (int phase = 0; phase < SoundtrackPlayer.HoldMusic.Phase.Count; phase++) {
						SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.Phase[phase][Random.Range(0, SoundtrackPlayer.HoldMusic.Phase[phase].Count)]); // Phase
						
						if(phase < SoundtrackPlayer.HoldMusic.PhaseTransition.Count && SoundtrackPlayer.HoldMusic.PhaseTransition[phase].Count > 0)
							SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.PhaseTransition[phase][Random.Range(0, SoundtrackPlayer.HoldMusic.PhaseTransition[phase].Count)]); // Phase Transition
					}
				}
				else { // Not using Phases. Continue as normal.
					SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.Lo[Random.Range(0, SoundtrackPlayer.HoldMusic.Lo.Length)]); // Lo
					if(SoundtrackPlayer.HoldMusic.Transition.Length > 0)
						SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.Transition[Random.Range(0, SoundtrackPlayer.HoldMusic.Transition.Length)]); // Transition
					if(SoundtrackPlayer.HoldMusic.MedHi.Length > 0)
						SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.MedHi[Random.Range(0, SoundtrackPlayer.HoldMusic.MedHi.Length)]); // MedHi
				}
				if(SoundtrackPlayer.HoldMusic.End.Length > 0)
					SoundtrackPlayer.Queue(SoundtrackPlayer.HoldMusic.End[Random.Range(0, SoundtrackPlayer.HoldMusic.End.Length)]); // End

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
				SoundtrackPlayer.HoldMusic = SoundtrackAPI.GetAudioclipsForHold(GM.TNH_Manager.m_level + 1);
				if (SoundtrackPlayer.HoldMusic.Take.Length == 0) {
					Debug.Log($"Getting audioclips for take {GM.TNH_Manager.m_level + 1}.");
					take = SoundtrackAPI.GetAudioclipsForTake(GM.TNH_Manager.m_level + 1).Track;
				}
				else {
					take = SoundtrackPlayer.HoldMusic.Take[Random.Range(0, SoundtrackPlayer.HoldMusic.Take.Length)];
				}

				SoundtrackPlayer.Queue(take);
				SoundtrackPlayer.PlayNextSongInQueue(); // Plays next song, finishing Take and playing Intro (if exists, if not, Lo or Phases.)
			}
			return false;
		}
	}
}