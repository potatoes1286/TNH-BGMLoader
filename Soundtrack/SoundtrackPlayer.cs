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
		
		public AudioSource GetNotCurrentAudioSource => AudioSources[CurrentAudioSource == 0 ? 1 : 0];
		public AudioSource GetCurrentAudioSource => AudioSources[CurrentAudioSource];

		public static List<Track> SongQueue = new List<Track>();
		public static Track       CurrentTrack;

		public static string GameMode;

		/// <summary>
		/// Queues a random track from a TrackSet, given a track type. Returns if successfully queued a track.
		/// </summary>
		/// <param name="set">TrackSet with tracks that can be chosen from.</param>
		/// <param name="type">The type the track must be to be added.</param>
		/// <param name="mandatory">If true, will throw an error if no tracks match the requirements. If false, will not.</param>
		public virtual bool QueueRandomOfType(TrackSet set, string type, bool mandatory = true) {
			var tracks = set.Tracks.Where(x => x.Type == type).ToArray();
			if (tracks.Any()) {
				QueueRandom(tracks);
				return true;
			}
			else {
				if (mandatory)
					PluginMain.DebugLog.LogError($"Track set {set.Name} did not contain mandatory track type {type}!");
				return false;
			}
		}

		/// <summary>
		/// Adds a single track from an array of tracks to the queue, selected at random by Unity's random generator.
		/// </summary>
		/// <param name="track">Array of tracks that can be picked from. Order not relevant.</param>
		public virtual void QueueRandom(Track[] track) {
			Queue(track[Random.Range(0, track.Length)]);
		}
		
		/// <summary>
		/// Adds several tracks to the queue.
		/// </summary>
		/// <param name="tracks">Array of tracks to be added to the queue. Items are added in order of index.</param>
		public virtual void QueueMany(Track[] tracks) {
			foreach (var track in tracks)
				Queue(track);
		}
		
		/// <summary>
		/// Adds a track to the song queue.
		/// </summary>
		/// <param name="track">Track to add to the queue.</param>
		public virtual void Queue(Track track) {
			PluginMain.DebugLog.LogInfo($"Queueing {track.Type}, {track.Name} of situation {track.Situation}");
			SongQueue.Add(track);
			// SongQueue.Count == 1 means this track that was just loaded is the first in the queue.
			if (SongQueue.Count == 1) //Preload the track before it is needed to prevent tiny gaps in audio.
				GetNotCurrentAudioSource.clip = track.Clip;
		}

		/// <summary>
		/// Removes all tracks in the queue. Does not affect currently played track.
		/// </summary>
		public virtual void ClearQueue() {
			SongQueue = new List<Track>();
		}

		private static bool  isSwitching; //if true, is switching between songs
		private static float switchStartTime; //When the switch began via Time.time
		public static float TrackFadeTime = 1.5f; //how long the switch lasts, in seconds
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
		
		/// <summary>
		/// Switches currently played track. Does not affect queue.
		/// </summary>
		/// <param name="newTrack">New track to play.</param>
		/// <param name="timeOverride">Sets playhead position on start, in seconds. Values under 0 are discarded.
		/// As an example, inputting 30 will cause the song to start playing 30 seconds into the song.</param>
		public virtual void SwitchSong(Track newTrack, float timeOverride = 0) {
			CurrentTrack = newTrack;
			
			bool loopNewSong = newTrack.Metadata.Any(x => x == "loop");
			bool fadeOut = newTrack.Metadata.All(x => x != "dnf");
			bool seamlessTransition = newTrack.Metadata.Any(x => x == "st");

			if(!seamlessTransition)
				SongStartTime = Time.time;
			var curTime = GetCurrentAudioSource.time;
			


			SongLength = newTrack.Clip.length;
			
			PluginMain.DebugLog.LogInfo($"{Time.time}: Playing track {newTrack.Name} ({newTrack.Type}) of calculated length {SongLength} with override time {timeOverride}.");

			//If current source is 0, new source is 1, and vice versa.
			int newSource = CurrentAudioSource == 0 ? 1 : 0;
			if (fadeOut) {
				switchStartTime = Time.time;
				CurrentAudioSource = newSource;
				isSwitching = true;
				GetCurrentAudioSource.clip = newTrack.Clip;
				GetCurrentAudioSource.volume = 0;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
			else {
				GetCurrentAudioSource.Stop();
				if (SongQueue.Count > 0) //Preload next track, if possible
					GetCurrentAudioSource.clip = SongQueue[0].Clip;
				CurrentAudioSource = newSource;
				if(GetCurrentAudioSource.clip != newTrack.Clip) //Check if it's preloaded
					GetCurrentAudioSource.clip = newTrack.Clip; //If not, load it
				GetCurrentAudioSource.volume = Volume;
				if (loopNewSong)
					GetCurrentAudioSource.loop = true;
				else
					GetCurrentAudioSource.loop = false;
				GetCurrentAudioSource.Play();
			}
			
			GetCurrentAudioSource.time = 0;
			if(seamlessTransition)
				GetCurrentAudioSource.time = curTime;
			if(timeOverride >= 0)
				GetCurrentAudioSource.time = timeOverride;
		}

		/// <summary>
		/// Must be called directly after instantiation, to prevent issues. Initializes the Soundtrack Player.
		/// </summary>
		/// <param name="gameMode">The current gamemode. As an example, Take and Hold is 'tnh'. Doesn't do
		/// anything right now.</param>
		/// <param name="soundtrack">Soundtrack to pull TrackSets and other info from. gameMode and soundtrack.GameMode
		/// should agree with each other.</param>
		/// <param name="trackFadeTime">Time allotted for tracks to fade into each other in seconds.
		/// Default is 1.5.</param>
		/// <param name="volume">Volume for the soundtrack to be played at. Ranges from 0-4, with 1 being the default
		/// value.</param>
		public void Initialize(string gameMode, SoundtrackManifest soundtrack, float trackFadeTime, float volume) {
			GameMode = gameMode; // this doesn't actually do anything but its a good sanity check i think
			Instance = this;
			TrackFadeTime = trackFadeTime;
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
				float progress = timeDif / TrackFadeTime; //pcnt progress on how far into switching it is
				float newVol = progress * Volume; //Linear progression sadly. TODO: Add easing function
				GetCurrentAudioSource.volume = newVol;
				GetNotCurrentAudioSource.volume = Volume - newVol;
				if (progress >= 1) {
					isSwitching = false;
					GetNotCurrentAudioSource.Stop();
				}
			}
			
			//Handle Queue
			timeDif = (float)SongLength - (Time.time - SongStartTime);
			
			
			float timeUntilStartNextSong = TrackFadeTime;
			if (SongQueue.Count > 0 && SongQueue[0].Metadata.Contains("dnf"))
				timeUntilStartNextSong = 0.05f; //Otherwise the last 1.5s will be cut off.
			//Note for a potential bug- songStartTime will NOT reset if the song is looping.
			if (timeDif <= timeUntilStartNextSong && !GetCurrentAudioSource.loop && SongQueue.Count > 0) {
				Debug.Log($"End of song incoming. Playing next song.");
				PlayNextSongInQueue();
			}
		}
		
		/// <summary>
		/// Plays the next song in the queue.
		/// </summary>
		public virtual void PlayNextSongInQueue() {
			//Swap the songs out with juuuust enough time for the current song to end.
			if (SongQueue.Count == 0)
				PluginMain.DebugLog.LogError("Ran out of songs in the queue!");
			else {
				var song = SongQueue[0];
				SongQueue.RemoveAt(0);
				SwitchSong(song);
			}
		}

		/// <summary>
		/// Plays the next track in the queue of an accepted type. If the next song is not the accepted
		/// type, it will remove the next queued item and continue until a track of the accepted type is found.
		/// Ensure that there is a track of the accepted type in the queue- or else there may be an error thrown.
		/// </summary>
		/// <param name="type">Accepted type.</param>
		public virtual void PlayNextTrackInQueueOfType(string type) => PlayNextTrackInQueueOfType(new[] { type });

		/// <summary>
		/// Plays the next track in the queue of a given set of accepted types. If the next song is not an accepted
		/// type, it will remove the next queued item and continue until a track of accepted type is found.
		/// Ensure that there is a track of accepted type in the queue- or else there may be an error thrown.
		/// </summary>
		/// <param name="types">Array of accepted types.</param>
		public virtual void PlayNextTrackInQueueOfType(string[] types) {
			while (!types.Contains(SongQueue[0].Type)) {
				Debug.Log($"Skipping song {SongQueue[0].Name} of type {SongQueue[0].Type}");
				SongQueue.RemoveAt(0);
			}
			PlayNextSongInQueue();
		}
	}
}