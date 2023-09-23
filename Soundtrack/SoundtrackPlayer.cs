using System.Collections.Generic;
using System.Linq;
using System.Web;
using FistVR;
using FMODUnity;
using HarmonyLib;
using TNH_BGLoader;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {

	//Runs TnH soundtracks. Needs refactoring. This is ugly as hell.
	public class SoundtrackPlayer : MonoBehaviour {

		public static SoundtrackPlayer? Instance;

		public static SoundtrackManifest? CurrentSoundtrack;
		
		public static AudioSource[] AudioSources;
		public static int           CurrentAudioSource;
		
		public static AudioSource GetNotCurrentAudioSource => AudioSources[CurrentAudioSource == 0 ? 1 : 0];
		public static AudioSource GetCurrentAudioSource => AudioSources[CurrentAudioSource];

		public static List<Track> SongQueue = new List<Track>();

		public static string GameMode;

		
		public virtual void QueueRandomOfType(TrackSet set, string type, bool mandatory = true) {
			var tracks = set.Tracks.Where(x => x.Type == type).ToArray();
			if(tracks.Any())
				QueueRandom(tracks);
			else if(mandatory)
				PluginMain.DebugLog.LogError($"Track set {set.Name} did not contain mandatory track type {type}!");
		}

		public virtual void QueueRandom(Track[] track) {
			SongQueue.Add(track[Random.Range(0, track.Length)]);
		}
		
		public virtual void QueueMany(Track[] tracks) {
			foreach (var track in tracks)
				SongQueue.Add(track);
		}
		
		public virtual void Queue(Track track) {
			SongQueue.Add(track);
		}

		public virtual void ClearQueue() {
			SongQueue = new List<Track>();
		}

		private static bool  isSwitching; //if true, is switching between songs
		private static float switchStartTime; //When the switch began via Time.time
		public static float SwitchLength = 1.5f; //how long the switch lasts, in seconds
		public static float Volume = 1; //volume set by settings (Maximum is naturally 1)
		
		public static double SongLength; // amount of time in seconds the current song lasts
		public static float  SongStartTime; // When the current song began via Time.time
		
		
		private static void CreateAudioSources() {
			AudioSources = new[] {
				GM.Instance.m_currentPlayerBody.Head.gameObject.AddComponent<AudioSource>(),
				GM.Instance.m_currentPlayerBody.Head.gameObject.AddComponent<AudioSource>()
			};
			foreach (var source in AudioSources) {
				source.playOnAwake = false;
				source.priority = 0;
				source.volume = Volume;
				source.spatialBlend = 0;
				source.loop = true; //lets just fucking assume huh
			}
		}
		
		public virtual void SwitchSong(Track newSong, float timeOverride = -1) {
			
			bool loopNewSong = newSong.Metadata.Any(x => x == "loop");
			bool fadeOut = newSong.Metadata.All(x => x != "dnf");
			bool seamlessTransition = newSong.Metadata.Any(x => x == "st");

			if(!seamlessTransition)
				SongStartTime = Time.time;
			var curTime = GetCurrentAudioSource.time;
			


			SongLength = newSong.Clip.length;
			
			Debug.Log($"Playing song {newSong.Name} of calculated length {SongLength} (naive time {newSong.Clip.length}).");

			//If current source is 0, new source is 1, and vice versa.
			int newSource = CurrentAudioSource == 0 ? 1 : 0;
			if (fadeOut) {
				switchStartTime = Time.time;
				CurrentAudioSource = newSource;
				isSwitching = true;
				GetCurrentAudioSource.clip = newSong.Clip;
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
				GetCurrentAudioSource.clip = newSong.Clip;
				GetCurrentAudioSource.volume = Volume;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
			
			if(seamlessTransition)
				GetCurrentAudioSource.time = curTime;
			if(timeOverride >= 0)
				GetCurrentAudioSource.time = timeOverride;
		}

		public void Initialize(string gameMode, SoundtrackManifest soundtrack, float switchLength, float volume) {
			GameMode = gameMode; // this doesn't actually do anything but its a good sanity check i think
			Instance = this;
			SwitchLength = switchLength;
			CurrentSoundtrack = soundtrack;
			Volume = volume;
			CreateAudioSources();
			
			//FMOD is muted in SoundtrackPatches on tnhmanager start.
			//For some reason TnHManager hasnt inited yet here, so itll null throw if i do it here.
			
			/*if (SoundtrackAPI.IsMix) {
				int num = Random.Range(0, SoundtrackAPI.Soundtracks.Count);
				SoundtrackAPI.SelectedSoundtrack = num;
			}*/
			
			//Load the soundtrack if it aint.
			//It should be loaded by now, actually. Just making sure.
			//TODO: Unload it when done
			if (!CurrentSoundtrack.Loaded)
				CurrentSoundtrack.AssembleMusicData();
		}

		public void Update() {
			//Update the song if it's currently fading in and out.
			float timeDif = 0;
			if (isSwitching) {
				timeDif = Time.time - switchStartTime; //get time dif between start and now
				float progress = timeDif / SwitchLength; //pcnt progress on how far into switching it is
				float newVol = progress * Volume; //Linear progression sadly. TODO: Add easing function
				GetCurrentAudioSource.volume = newVol;
				GetNotCurrentAudioSource.volume = Volume - newVol;
				if (progress >= 1) {
					isSwitching = false;
					GetNotCurrentAudioSource.Stop();
					Debug.Log($"Phaseout complete at {Time.time}. Current track time: {GetCurrentAudioSource.time}");
				}
			}
			
			//Handle Queue
			timeDif = (float)SongLength - (Time.time - SongStartTime);
			//Note for a potential bug- songStartTime will NOT reset if the song is looping.
			if (timeDif <= SwitchLength && !GetCurrentAudioSource.loop && SongQueue.Count > 0) {
				Debug.Log($"End of song incoming. Playing next song.");
				PlayNextSongInQueue();
			}
		}
		
		//Gets next item in queue and plays it. Simple as.
		public virtual void PlayNextSongInQueue() {
			//Swap the songs out with juuuust enough time for the current song to end.
			var song = SongQueue[0];
			SongQueue.RemoveAt(0);
			SwitchSong(song);
		}
	}
}