using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FistVR;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	
	//Handles much of the backend workings of soundtracks.
	public static class SoundtrackAPI {

		public static List<SoundtrackManifest> Soundtracks = new List<SoundtrackManifest>();
		//See PluginMain.IsSoundtrack.Value for var to check if is soundtrack.
		public static int  SelectedSoundtrackIndex;
		public static bool IsMix;

		public static SoundtrackManifest GetCurrentSoundtrack => Soundtracks[SelectedSoundtrackIndex];



		//Assemble a complete soundtrack manifest using the path of the file.
		//Can be written as Ass Music for short, symbolizing what you're gonna do with it.
		public static void AssembleMusicData(this SoundtrackManifest manifest) {
			//Get path of the soundtrack.
			string dirPath = Path.Combine(Path.GetDirectoryName(manifest.Path), manifest.Location);
			//Get a list of all the folders in the soundtrack folder.
			string[] rawSets = Directory.GetDirectories(dirPath);
			
			//All the sets assembled.
			var sets = new List<TrackSet>();
			foreach (var rawSet in rawSets) {
				//Standard format of a set [Type]_[Situation]_[Metadata1]-[Metadata2]..._[Name]
				//Metadata part is optional. Sans metadata, [Type]_[Situation]_[Name]
				string[] splitName = Path.GetFileName(rawSet).Split('_');
				if (File.Exists(rawSet) && !rawSet.Contains(".ogg")) //it was ingesting the fucking yaml :/
					continue;
				var set = new TrackSet();
				set.Tracks = new List<Track>();
				set.Type = splitName[0];
				set.Situation = splitName[1];
				if (splitName.Length == 3) { //if metadata does not exist
					set.Metadata = new[] { "" }; //i don wanna deal w nulls
					set.Name = splitName[2];
				}
				else { //if metadata exists
					set.Metadata = splitName[2].Split('-');
					set.Name = splitName[3];
				}
				
				// Go thru all tracks in each folder
				var rawTrackLocations = Directory.GetFiles(rawSet, "*.ogg", SearchOption.TopDirectoryOnly);
				foreach (var rawTrackLocation in rawTrackLocations) {
					//Standard format of a track [Type]_[Metadata1]-[Metadata2]..._[Name]
					//Metadata part is optional. Sans metadata, [Type]_[Name]
					var fileName = Path.GetFileName(rawTrackLocation);
					var track = new Track();
					if (Path.GetExtension(fileName) != ".ogg")
						PluginMain.DebugLog.LogError($"{fileName} has an invalid extension! (Valid extensions: .ogg, file extension: {Path.GetExtension(fileName)})");
					else
						track.Clip = LoadOgg(rawTrackLocation);
					
					string[] splitTrackName = Path.GetFileNameWithoutExtension(rawTrackLocation).Split('_');
					track.Type = splitTrackName[0];
					track.Situation = set.Situation; //Copy over the set situation info into here, just in case its needed.
					if (splitTrackName.Length == 2) { //metadata does not exist
						track.Metadata = new[] { "" }; //i don wanna deal w nulls
						track.Name = splitTrackName[1];
					}
					else { //metadata exists
						track.Metadata = splitTrackName[1].Split('-');
						track.Name = splitTrackName[2];
					}
					set.Tracks.Add(track);
				}
				sets.Add(set);
			}
			
			//logging
			foreach (var set in sets) {
				PluginMain.DebugLog.LogInfo($"Loading set {set.Name}, {set.Type}, {set.Tracks.Count}, {set.Situation}");
			}
			
			
			manifest.Sets = sets;
			manifest.Loaded = true;
		}

		//Convert a Yamlfest into a Manifest. As Yamlfest doesnt contain its path, it gotta be manually added.
		public static SoundtrackManifest ToManifest(this SoundtrackYamlfest yamlfest, string path) {
			var manifest = new SoundtrackManifest();
			manifest.Name = yamlfest.Name;
			manifest.Guid = yamlfest.Guid;
			manifest.Path = path;
			manifest.Location = yamlfest.Location;
			manifest.Loaded = false;
			manifest.GameMode = yamlfest.GameMode;
			return manifest;
		}
		
		//Loads new soundtrack to be ran.
		public static void LoadSoundtrack(int index) {
			//Flag the game that we're doing soundtrack. Unflagging is done in BankAPI.SwapBanks.
			PluginMain.IsSoundtrack.Value = true;
			SelectedSoundtrackIndex = index;
			//PluginMain.LastLoadedSoundtrack.Value = Soundtracks[SelectedSoundtrackIndex].Guid;
		}

		public static TrackSet GetSet(string type, int situation) {
			PluginMain.DebugLog.LogInfo($"Getting set of type {type}, {situation}. Cur soundtrack: {Soundtracks[SelectedSoundtrackIndex].Guid}");
			var soundtrack = Soundtracks[SelectedSoundtrackIndex];
			var sets = soundtrack.Sets
			   .Where(x => x.Type == type)
			   .Where(x => x.Situation.TimingsMatch(situation))
			   .ToArray();
			if(!sets.Any()) //If there are no sets that match, get fallback ones.
				sets = soundtrack.Sets
				   .Where(x => x.Type == type)
				   .Where(x => x.Situation == "fallback")
				   .ToArray();
			if(!sets.Any())
				PluginMain.DebugLog.LogError("No set!");
			var setIndex = UnityEngine.Random.Range(0, sets.Length);
			return sets[setIndex];
		}

		/*//I am a big hater of DRY
		//no particular reason 
		public static HoldData GetAudioclipsForHold(int situation) {
			var soundtrack = Soundtracks[SelectedSoundtrackIndex];
			var holds = soundtrack.Holds.Where(x => x.Timing.TimingsMatch(situation));
			if (holds.Count() == 0)
				holds = soundtrack.Holds.Where(x => x.Timing == "fallback");

			if (!holds.Any()) {
				holds = soundtrack.Holds.Where(x => x.Timing.TimingsMatch(0));
				if (situation < 4)
					Debug.LogError($"PTNHBGML: {soundtrack.Guid} does not have a take song for hold {situation}!");
			}

			var number = UnityEngine.Random.Range(0, holds.Count());
			var data = holds.ToArray()[number];
			Debug.Log($"Selected {data.Name}");
			return data;
		}
		
		public static TakeData GetAudioclipsForTake(int situation) {
			var soundtrack = Soundtracks[SelectedSoundtrackIndex];
			var takes = soundtrack.Takes.Where(x => x.Timing.TimingsMatch(situation));
			if (!takes.Any())
				takes = soundtrack.Takes.Where(x => x.Timing == "fallback");
			//Fallback if there are no fallbacks + no available tracks
			if (!takes.Any()) {
				takes = soundtrack.Takes.Where(x => x.Timing.TimingsMatch(0));
				if(situation < 4)
					Debug.LogError($"PTNHBGML: {soundtrack.Guid} does not have a take song for take {situation}!");
			}
			var number = UnityEngine.Random.Range(0, takes.Count());
			var data = takes.ToArray()[number];
			
			Debug.Log($"Selected {data.Name}");
			return data;
		}*/
		
		//Does not account for fallback.
		//Compares sequencetiming format (See _FORMAT.txt) and the current situation provided by TnH
		//Mainly to handle globs
		public static bool TimingsMatch(this string seqTiming, int situation) {
			//This is a parse hell.
			//There's probably a better way to do this
			if (seqTiming == "all")
				return true;
			if (seqTiming == "fallback")
				return false;
			if (seqTiming == "death") {
				if (situation == -1)
					return true;
				return false;
			}
			if (seqTiming == "win") {
				if (situation == -2)
					return true;
				return false;
			}


			if (seqTiming.Contains(',')) {
				//This is a split seqTiming. EG 1,3,5
				var situations = seqTiming.Split(',');
				if (situations.Contains(situation.ToString()))
					return true;
				return false;
			}

			if (seqTiming.Contains('-')) {
				//This is a range. EG 1-3
				var situations = seqTiming.Split('-');
				if (situation >= int.Parse(situations[0]) && situation <= int.Parse(situations[1]))
					return true;
				return false;
			}

			if (seqTiming.Contains("ge")) {
				//This is above a number (inclusive). EG ge3
				var val = int.Parse(seqTiming.Replace("ge", string.Empty));
				if (situation >= val)
					return true;
				return false;
			}
			
			if (seqTiming.Contains("le")) {
				//This is above a number (inclusive). EG le3
				var val = int.Parse(seqTiming.Replace("le", string.Empty));
				if (situation <= val)
					return true;
				return false;
			}

			if (situation == int.Parse(seqTiming))
				return true;
			return false;
		}

		public static void EnableSoundtrackFromGUID(string guid) {
			for (int i = 0; i < Soundtracks.Count; i++)
				if (Soundtracks[i].Guid == guid)
					SelectedSoundtrackIndex = i;
		}

		public static Texture2D GetIcon(int soundtrack) {
			return GeneralAPI.GetIcon(Soundtracks[soundtrack].Guid, new[] { Path.Combine(Path.GetDirectoryName(Soundtracks[soundtrack].Path), "icon.png") });
		}

		public static AudioClip LoadOgg(string path) {
			WWW www = new WWW($"file://{path}");
			for (int t = 0; t < 500; t++) {
				if (www.isDone)
					break;
				Thread.Sleep(10); //Multithreading? never heard o' her!
			}
			AudioClip clip = www.GetAudioClip(false);
			clip.name = Path.GetFileName(path);
			return clip;
		}
		
		public static ValueTuple<AudioClip?, bool> GetSnippet(SoundtrackManifest manifest) {
			bool isLoop = true;
			string pathOgg = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path)!, manifest.Location), "preview_loop.ogg");
			if (!File.Exists(pathOgg)) {
				pathOgg = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path)!, manifest.Location), "preview.ogg");
				isLoop = false;
			}

			AudioClip? clip = null;
			if(File.Exists(pathOgg))
				clip = LoadOgg(pathOgg);
			return (clip, isLoop);
		}
	}
}