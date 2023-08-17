using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	public static class SoundtrackAPI {

		public static SoundtrackManifest[] Soundtracks;
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
	}
}