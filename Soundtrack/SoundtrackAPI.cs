using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FistVR;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	public static class SoundtrackAPI {

		public static SoundtrackManifest[] Soundtracks;
		//If enabled, use soundtracks. If not, use Banks.
		public static bool                 SoundtrackEnabled;
		public static int                  SelectedSoundtrack;


		
		//Assemble a complete soundtrack manifest using the path of the file.
		//Can be written as Ass Music for short, symbolizing what you're gonna do with it.
		public static SoundtrackManifest AssembleMusicData(SoundtrackManifest manifest) {
			string dirPath = manifest.Path;
			//ingest sequences
			List<HoldData> sequenceDatas = new List<HoldData>();
			string[] sequences = Directory.GetDirectories(dirPath);
			foreach (var sequence in sequences) {
				//split directory name into sequence, timing, and name
				//See _FORMAT.txt for more.
				string[] metadata = Path.GetDirectoryName(sequence).Split('_');
				//Verify the sequence info is valid
				if (metadata[0] != "sequence" || metadata.Length != 3) {
					Debug.LogError($"Soundtrack {manifest.Name} has an incorrect sequence info name at file {sequence}!");
					continue;
				}
				HoldData data = new HoldData();
				//Fill in the metadata for timing and name.
				data.Timing = metadata[1];
				data.Name = metadata[2];
				//get the wav files; turn to audio clips and all that jazz
				data.Intro = WavUtility.ToAudioClip(Path.Combine(sequence, "intro.wav"));
				data.Lo = WavUtility.ToAudioClip(Path.Combine(sequence, "lo.wav"));
				data.Transition = WavUtility.ToAudioClip(Path.Combine(sequence, "transition.wav"));
				data.MedHi = WavUtility.ToAudioClip(Path.Combine(sequence, "medhi.wav"));
				data.End = WavUtility.ToAudioClip(Path.Combine(sequence, "end.wav"));
				sequenceDatas.Add(data);
			}
			
			//ingest takes
			List<TakeData> takeDatas = new List<TakeData>();
			//Get all the files that match the glob format of a take file (take_[timing]_[name])
			//See _FORMAT.txt for more info.
			string[] takes = Directory.GetFiles(dirPath, "take_*_*.wav", SearchOption.TopDirectoryOnly);
			foreach (var take in takes) {
				string[] metadata = Path.GetFileName(take).Split('_');
				TakeData data = new TakeData();
				data.Timing = metadata[1];
				data.Name = metadata[2];
				data.Track = WavUtility.ToAudioClip(take);
				takeDatas.Add(data);
			}
			manifest.Holds = sequenceDatas.ToArray();
			manifest.Takes = takeDatas.ToArray();
			return manifest;
		}
		
		//Convert a Yamlfest into a Manifest. As Yamlfest doesnt contain its path, it gotta be manually added.
		public static SoundtrackManifest ToManifest(this SoundtrackYamlfest yamlfest, string path) {
			var manifest = new SoundtrackManifest();
			manifest.Name = yamlfest.Name;
			manifest.Guid = yamlfest.Guid;
			manifest.Path = path;
			return manifest;
		}
		
		//Loads new soundtrack to be ran.
		public static void LoadSoundtrack(int index) {
			//Flag the game that we're doing soundtrack. Unflagging is done in BankAPI.SwapBanks.
			SoundtrackEnabled = true;
			SelectedSoundtrack = index;
			//Do i even have to do more?
			//Uh.
		}

		
		//I am a big hater of DRY
		//no particular reason
		public static HoldData GetAudioclipsForHold(int situation) {
			var soundtrack = Soundtracks[SelectedSoundtrack];
			var holds = soundtrack.Holds.Where(x => x.Timing.TimingsMatch(situation));
			if (holds.Count() == 0)
				holds = soundtrack.Holds.Where(x => x.Timing == "fallback");
			var number = UnityEngine.Random.Range(0, holds.Count());
			return holds.ToArray()[number];
		}
		
		public static TakeData GetAudioclipsForTake(int situation) {
			var soundtrack = Soundtracks[SelectedSoundtrack];
			var takes = soundtrack.Takes.Where(x => x.Timing.TimingsMatch(situation));
			if (takes.Count() == 0)
				takes = soundtrack.Takes.Where(x => x.Timing == "fallback");
			var number = UnityEngine.Random.Range(0, takes.Count());
			return takes.ToArray()[number];
		}
		
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

			if (seqTiming.Contains('+')) {
				//This is above a number (inclusive). EG 3+
				var val = int.Parse(seqTiming.Replace("+", string.Empty));
				if (situation >= val)
					return true;
				return false;
			}
			
			if (seqTiming.Contains('-')) {
				//This is above a number (inclusive). EG 3+
				var val = int.Parse(seqTiming.Replace("-", string.Empty));
				if (situation <= val)
					return true;
				return false;
			}

			if (situation == int.Parse(seqTiming))
				return true;
			return false;
		}

		public static Texture2D GetIcon(int soundtrack) {
			//TODO: DO this!
			return null;
		}
	}
}