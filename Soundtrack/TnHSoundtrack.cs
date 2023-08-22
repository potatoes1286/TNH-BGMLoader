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
				metadata = metadata.Split('/'),
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
		private static float songStartTime; // When the current song began via Time.time


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
		
		public static void SwitchSong(AudioClip newSong, string name, string[] metadata) {
			
			bool loopNewSong = metadata.Any(x => x == "loop");
			bool fadeOut = metadata.All(x => x != "dnf");
			bool seamlessTransition = metadata.Any(x => x == "st");
			
			if(!seamlessTransition)
				songStartTime = Time.time;
			var curTime = GetCurrentAudioSource.time;

			// Who the fuck decided that the length of an audioclip would not represent the length of an audioclip?
			// I hate unity with a burning passion.
			//songLength = newSong.length;
			songLength = (double)newSong.samples / (newSong.frequency * newSong.channels);
			
			Debug.Log($"Playing song {name} of length {songLength} ");

			//If current source is 0, new source is 1, and vice versa.
			int newSource = CurrentAudioSource == 0 ? 1 : 0;
			if (fadeOut) {
				switchStartTime = Time.time;
				CurrentAudioSource = newSource;
				isSwitching = true;
				GetCurrentAudioSource.clip = newSong;
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
				GetCurrentAudioSource.clip = newSong;
				GetCurrentAudioSource.volume = vol;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}

			GetCurrentAudioSource.time = curTime;
		}

		public void Awake() {
			vol = PluginMain.AnnouncerMusicVolume.Value / 4f;
			CreateAudioSources();
			//I'm not too sure why? But hotswapping banks mid-game will completely break FMOD.
			//In other words, instead of properly stopping FMOD, i am just making itself crash.
			//Good code design.
			Debug.Log("Gonna make it play now.");
			SwitchSong(SoundtrackAPI.GetAudioclipsForTake(0).Track, "Take", new [] {"loop"}); //start playing take theme
		}

		public void Update() {
			//Update the song if it's currently fading in and out.
			float timeDif = 0;
			if (isSwitching) {
				timeDif = Time.time - switchStartTime; //get time dif between start and now
				float progress = timeDif / switchLength; //pcnt progress on how far into switching it is
				float newVol = progress * vol; //Linear progression sadly. TODO: Add easing function
				GetCurrentAudioSource.volume = newVol;
				GetNotCurrentAudioSource.volume = 1 - newVol;
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
			else if (timeDif <= 0f && GetCurrentAudioSource.loop) {
				//Handle looping. because the BUILT IN LOOP FUNCTION FOR UNITY DOESN'T ACTUALLY GODDAMN WORK PROPERLY
				//UUUUUGGGHHH UNITY PLEASE
				GetCurrentAudioSource.Stop();
				GetCurrentAudioSource.Play();
				songStartTime = Time.time;
			}
		}
		
		//Gets next item in queue and plays it. Simple as.
		public static void PlayNextSongInQueue() {
			//Swap the songs out with juuuust enough time for the current song to end.
			var song = SongQueue[0];
			SongQueue.RemoveAt(0);
			SwitchSong(song.clip, song.name, song.metadata);
		}
	}
}