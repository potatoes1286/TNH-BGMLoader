using System.Collections.Generic;
using System.Linq;
using FistVR;
using FMODUnity;
using HarmonyLib;
using TNH_BGLoader;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {

	public class TnHSoundtrack : MonoBehaviour {
		
		public static AudioSource[] AudioSources;
		public static int           CurrentAudioSource;
		
		public static AudioSource GetNotCurrentAudioSource => AudioSources[CurrentAudioSource == 0 ? 1 : 0];
		public static AudioSource GetCurrentAudioSource => AudioSources[CurrentAudioSource];

		public static List<Track> SongQueue = new List<Track>();

		//feels redundant, probably is
		public static void Queue(AudioClip clip, string metadata, string name, string type) {
			SongQueue.Add(new Track
			{
				clip = clip,
				metadata = metadata.Split('-'),
				name = name,
				type = type
			});
		}
		
		//feels redundant, probably is
		public static void Queue(AudioClip clip, string[] metadata, string name, string type) {
			SongQueue.Add(new Track
			{
				clip = clip,
				metadata = metadata,
				name = name,
				type = type
			});
		}
		
		//oh now THIS is redundant.
		public static void Queue(Track track) {
			SongQueue.Add(track);
		}

		private static bool  isSwitching; //if true, is switching between songs
		private static float switchStartTime; //When the switch began via Time.time
		private static float switchLength = 1.5f; //how long the switch lasts, in seconds
		private static float vol = (PluginMain.AnnouncerMusicVolume.Value / 4f); //volume set by settings (Maximum is naturally 1)
		
		private static double songLength; // amount of time in seconds the current song lasts
		private static float  songStartTime; // When the current song began via Time.time
		
		//Failure sync stuff.
		//A flip to let the switchsong know that the failruesync info is ready.
		public static  bool   failureSyncInfoReady;
		public static  float  timeIdentified;
		public static  float  timeFail;


		public static void CreateAudioSources() {
			AudioSources = new[] {
				GM.Instance.m_currentPlayerBody.Head.gameObject.AddComponent<AudioSource>(),
				GM.Instance.m_currentPlayerBody.Head.gameObject.AddComponent<AudioSource>()
			};
			foreach (var source in AudioSources) {
				source.playOnAwake = false;
				source.priority = 0;
				source.volume = vol;
				source.spatialBlend = 0;
				source.loop = true; //lets just fucking assume huh
			}
		}
		
		public static void SwitchSong(Track newSong) {
			
			bool loopNewSong = newSong.metadata.Any(x => x == "loop");
			bool fadeOut = newSong.metadata.All(x => x != "dnf");
			bool seamlessTransition = newSong.metadata.Any(x => x == "st");
			bool failureSync = newSong.metadata.Any(x => x == "fs");
			

			if(!seamlessTransition)
				songStartTime = Time.time;
			var curTime = GetCurrentAudioSource.time;
			

			// Who the fuck decided that the length of an audioclip would not represent the length of an audioclip?
			// I hate unity with a burning passion.
			//songLength = newSong.length;
			if(newSong.format == "wav")
				songLength = (double)newSong.clip.samples / (newSong.clip.frequency * newSong.clip.channels);
			else if (newSong.format == "ogg")
				songLength = newSong.clip.length;
			
			Debug.Log($"Playing song {newSong.name} of calculated length {songLength} (naive time {newSong.clip.length})");

			//If current source is 0, new source is 1, and vice versa.
			int newSource = CurrentAudioSource == 0 ? 1 : 0;
			if (fadeOut) {
				switchStartTime = Time.time;
				CurrentAudioSource = newSource;
				isSwitching = true;
				GetCurrentAudioSource.clip = newSong.clip;
				GetCurrentAudioSource.volume = 0;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
			else {
				GetCurrentAudioSource.Stop();
				CurrentAudioSource = newSource;
				GetCurrentAudioSource.clip = newSong.clip;
				GetCurrentAudioSource.volume = vol;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
			
			if(seamlessTransition)
				GetCurrentAudioSource.time = curTime;
			if (failureSync) {
				//The info is already there and waiting for us.
				if (failureSyncInfoReady) {
					float timeToFail = (timeFail - timeIdentified) - (Time.time - timeIdentified);
					//Ensure the song is long enough.
					if (timeToFail > songLength) {
						PluginMain.DebugLog
.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrack]}:{newSong.name} is TOO SHORT! Song length: {songLength}, Time to Fail: {timeToFail}! Lengthen your song!");
						return;
					}
					double playHead = songLength - timeToFail;
					GetCurrentAudioSource.time = (float)playHead;
					failureSyncInfoReady = false;
				}
				//It hasn't been identified yet. Just fucking throw.
				else {
					PluginMain.DebugLog.LogError($"Soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrack]}:{newSong.name} DID NOT have enough time to get info about how long the hold is! (FailureSync). Please lengthen your transition or intro to give more buffer time for the info to load! It should be AT LEAST 5 seconds.");
				}
			}
		}

		public void Awake() {
			vol = PluginMain.AnnouncerMusicVolume.Value / 4f;
			CreateAudioSources();
			//I'm not too sure why? But hotswapping banks mid-game will completely break FMOD.
			//In other words, instead of properly stopping FMOD, i am just making itself crash.
			//Good code design.

			//For some weird reason, this runs before TnHSoundtrack Awake.
			if (SoundtrackAPI.IsMix) {
				int num = Random.Range(0, SoundtrackAPI.Soundtracks.Count);
				SoundtrackAPI.SelectedSoundtrack = num;
			}

			Debug.Log("Gonna make it play now.");
			
			var takeData = SoundtrackAPI.GetAudioclipsForTake(0);
			SwitchSong(takeData.Track); //start playing take theme
		}

		public void Update() {
			//Update the song if it's currently fading in and out.
			float timeDif = 0;
			if (isSwitching) {
				timeDif = Time.time - switchStartTime; //get time dif between start and now
				float progress = timeDif / switchLength; //pcnt progress on how far into switching it is
				float newVol = progress * vol; //Linear progression sadly. TODO: Add easing function
				GetCurrentAudioSource.volume = newVol;
				GetNotCurrentAudioSource.volume = vol - newVol;
				if (progress >= 1) {
					isSwitching = false;
					GetNotCurrentAudioSource.Stop();
				}
			}
			
			//Handle Queue
			timeDif = (float)songLength - (Time.time - songStartTime);
			//Note for a potential bug- songStartTime will NOT reset if the song is looping.
			if (timeDif <= switchLength && !GetCurrentAudioSource.loop && SongQueue.Count > 0) {
				Debug.Log($"End of song incoming. Playing next song.");
				PlayNextSongInQueue();
			}
			// There's some natural variance when this is run roughly equal to 0.05s.
			// We need this buffer or else there's a tiny time where there is no song playing and its a lil jarring
			// Okay, minor update. Wav files have an incorrect clip length according to unity, so we have to manually loop it.
			// OGG length actually aligns with Unity's calculations, so we can let unity handle looping if its an OGG.
			// We don't know if its a WAV or OGG at this time (whoops) so we just check if songLength and Unity's length are close enough:tm:
			// And if it is, assume OGG and don't run this code.
			else if (timeDif <= 0.05f && GetCurrentAudioSource.loop && !(Mathf.Abs(GetCurrentAudioSource.clip.length - (float)songLength) <= 0.01)) {
				Debug.Log($"Looping at {GetCurrentAudioSource.time}");
				//Handle looping. because the BUILT IN LOOP FUNCTION FOR UNITY DOESN'T ACTUALLY GODDAMN WORK PROPERLY
				//UUUUUGGGHHH UNITY PLEASE
				GetCurrentAudioSource.time = 0;
				songStartTime = Time.time;
			}
		}
		
		//Gets next item in queue and plays it. Simple as.
		public static void PlayNextSongInQueue() {
			//Swap the songs out with juuuust enough time for the current song to end.
			var song = SongQueue[0];
			SongQueue.RemoveAt(0);
			SwitchSong(song);
		}
	}
}