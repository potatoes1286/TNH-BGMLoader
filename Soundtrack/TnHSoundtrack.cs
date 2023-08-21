using System.Collections.Generic;
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
		public static AudioSource GetCurrentAudioSource => GetCurrentAudioSource;

		public struct SongQueueItem {
			public AudioClip clip;
			public bool      fadeIn;
			public bool      loop;
			public string    name;
		}
		
		public static List<SongQueueItem> SongQueue;

		//feels redundant, probably is
		public static void Queue(AudioClip clip, bool fadeIn, bool loop, string name) {
			SongQueue.Add(new SongQueueItem()
			{
				clip = clip,
				fadeIn = fadeIn,
				loop = loop,
				name = name
			});
		}

		private static bool  isSwitching; //if true, is switching between songs
		private static float switchStartTime; //When the switch began via Time.time
		private static float switchLength = 1.5f; //how long the switch lasts, in seconds
		private static float maxVol; //Maximum volume set by settings
		
		private static float songLength; // amount of time in seconds the current song lasts
		private static float songStartTime; // When the current song began via Time.time


		public static void CreateAudioSources() {
			AudioSources = new[] {
				GM.Instance.gameObject.AddComponent<AudioSource>(),
				GM.Instance.gameObject.AddComponent<AudioSource>()
			};
			foreach (var source in AudioSources) {
				source.playOnAwake = false;
				source.priority = 0;
				source.volume = maxVol;
				source.spatialBlend = 0;
				source.loop = true; //lets just fucking assume huh
			}
		}
		
		public static void SwitchSong(AudioClip newSong, bool loopNewSong = true, bool fadeOut = true) {
			songLength = newSong.length;
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
				GetCurrentAudioSource.volume = maxVol;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
		}

		public void Start() {
			CreateAudioSources();
			//I'm not too sure why? But hotswapping banks mid-game will completely break FMOD.
			//In other words, instead of properly stopping FMOD, i am just making itself crash.
			//Good code design.
			BankAPI.SwapBank(0);
			SwitchSong(SoundtrackAPI.GetAudioclipsForTake(0).Track); //start playing take theme
		}

		public void Update() {
			//Update the song if it's currently fading in and out.
			float timeDif = 0;
			if (isSwitching) {
				timeDif = Time.time - switchStartTime; //get time dif between start and now
				float progress = timeDif / switchLength; //pcnt progress on how far into switching it is
				float newVol = progress * maxVol; //Linear progression sadly. TODO: Add easing function
				GetCurrentAudioSource.volume = newVol;
				GetNotCurrentAudioSource.volume = 1 - newVol;
				if (progress >= 1) {
					isSwitching = false;
					GetNotCurrentAudioSource.Stop();
				}
			}
			
			//Handle Queue
			timeDif = songLength - (Time.time - songStartTime);
			//Note for a potential bug- songStartTime will NOT reset if the song is looping.
			if (timeDif <= switchLength && !GetCurrentAudioSource.loop && SongQueue.Count > 0) {
				PlayNextSongInQueue();
			}
		}
		
		//Gets next item in queue and plays it. Simple as.
		public static void PlayNextSongInQueue() {
			//Swap the songs out with juuuust enough time for the current song to end.
			var song = SongQueue[0];
			SongQueue.RemoveAt(0);
			SwitchSong(song.clip, song.loop, song.fadeIn);
		}
	}
}