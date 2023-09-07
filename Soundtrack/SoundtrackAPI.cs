using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FistVR;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	public static class SoundtrackAPI {

		public static List<SoundtrackManifest> Soundtracks = new List<SoundtrackManifest>();
		//See PluginMain.IsSoundtrack.Value for var to check if is soundtrack.
		public static int  SelectedSoundtrackIndex;
		public static bool IsMix;

		public static SoundtrackManifest GetCurrentSoundtrack => Soundtracks[SelectedSoundtrackIndex];


		//Assemble a complete soundtrack manifest using the path of the file.
		//Can be written as Ass Music for short, symbolizing what you're gonna do with it.
		public static void AssembleMusicData(this SoundtrackManifest manifest) {
			string dirPath = Path.Combine(Path.GetDirectoryName(manifest.Path), manifest.Location);
			//ingest sequences
			List<HoldData> sequenceDatas = new List<HoldData>();
			string[] sequences = Directory.GetDirectories(dirPath);
			foreach (var sequence in sequences) {
				Debug.Log($"Ingesting hold sequence {sequence}");
				if (File.Exists(sequence) && !sequence.Contains(".wav")) //it was ingesting the fucking yaml :/
					continue;
				//split directory name into sequence, timing, and name
				//See _FORMAT.txt for more.
				//Despite the confusing name, GetFileName works here. Changes C:\folder1\folder2 -> folder2
				string[] metadata = Path.GetFileName(sequence).Split('_');
				//Verify the sequence info is valid
				if (metadata[0] != "sequence" || metadata.Length != 3) {
					Debug.LogError($"Soundtrack {manifest.Name} has an incorrect sequence info name at file {Path.GetFileName(sequence)}!");
					continue;
				}
				HoldData data = new HoldData();
				//Fill in the metadata for timing and name.
				data.Timing = metadata[1];
				data.Name = metadata[2];
				//get the wav files; turn to audio clips and all that jazz
				
				//Put them all into lists before we populate the manifest arrays
				var Intros = new List<Track>();
				var Los = new List<Track>();
				var Transitions = new List<Track>();
				var MedHis = new List<Track>();
				var Ends = new List<Track>();
				var EndFails = new List<Track>();
				var Phases = new List<List<Track>>(); //2d list!
				var PhaseTransitions = new List<List<Track>>();
				var OrbActivates = new List<Track>();
				var OrbWave = new List<Track>();
				var OrbSuccess = new List<Track>();
				var OrbFailure = new List<Track>();
				
				// Go thru all .wavs and .mp3s and sort them with metadata, name, and track
				var files = Directory.GetFiles(sequence, "*.wav", SearchOption.TopDirectoryOnly);
				files = files.Concat(Directory.GetFiles(sequence, "*.ogg", SearchOption.TopDirectoryOnly)).ToArray();
				foreach (var file in files) {
					Debug.Log($"Handling file {file}");
					var fileName = Path.GetFileNameWithoutExtension(file);
					var ext = Path.GetExtension(file);
					var track = new Track();

					if (ext == ".wav") {
						track.clip = WavUtility.ToAudioClip(file);
						track.format = "wav";
					}
					else if (ext == ".ogg") {
						track.clip = LoadOgg(file);
						track.format = "ogg";
					}
					else
						PluginMain.DebugLog.LogError($"{file} has an invalid extension! (Valid extensions: .ogg, .wav)");


					var fileSplit = fileName.Split('_'); // Format: [Track type]_[metadata]_[identifier] or [Track type]_[identifier]
					//populate metadata + name fields
					if (fileSplit.Length == 2) {
						track.metadata = new[] { "" };
						track.name = fileSplit[1];
					} else if (fileSplit.Length == 3) {
						track.metadata = fileSplit[1].Split('-');
						track.name = fileSplit[2];
						//Verify the arguments in metadata are valid
						foreach (var arg in track.metadata)
							if (arg != "st" && arg != "dnf" && arg != "loop" && arg != "fs")
								Debug.LogError($"File {file} has invalid metadata: {arg}!");
					}
					else {
						Debug.LogError($"File {file} has an invalid name!");
					}
					//put them into the correct track types
					track.type = fileSplit[0];
					switch (fileSplit[0]) {
						case "intro":
							Intros.Add(track);
							break;
						case "lo":
							Los.Add(track);
							break;
						case "transition":
							Transitions.Add(track);
							break;
						case "medhi":
							MedHis.Add(track);
							break;
						case "end":
							Ends.Add(track);
							break;
						case "endfail":
							EndFails.Add(track);
							break;
						case "orbactivate":
							OrbActivates.Add(track);
							break;
						case "orbwave":
							OrbWave.Add(track);
							break;
						case "orbsuccess":
							OrbSuccess.Add(track);
							break;
						case "orbfailure":
							OrbFailure.Add(track);
							break;
						default:
							//handle phases
							var isTransition = false;
							//remove non-number parts
							string phaseNumber = fileSplit[0];
							if (fileSplit[0].Contains("phasetr")) {
								phaseNumber = fileSplit[0].Replace("phasetr", String.Empty);
								isTransition = true;
							} else if (fileSplit[0].Contains("phase")) {
								phaseNumber = fileSplit[0].Replace("phase", String.Empty);
							}
							var tp = int.TryParse(phaseNumber, out int phase);
							if (!tp) {
								Debug.LogError($"Cannot categorize {file}!");
								break;
							}
							//There must be 1 more Phase.Count than phase. phase starts at 0
							//If there's not enough Phases, then we just keep adding until we have enough
							if (!isTransition) {
								while (phase >= Phases.Count)
									Phases.Add(new List<Track>());
								Phases[phase].Add(track);
							}
							else {
								while (phase >= PhaseTransitions.Count)
									PhaseTransitions.Add(new List<Track>());
								PhaseTransitions[phase].Add(track);
							}

							break;
					}
				}
				
				if((Phases.Any() || PhaseTransitions.Any()) && (Los.Any() || MedHis.Any()))
					PluginMain.DebugLog.LogError($"{manifest.Guid}:{sequence} mixes together Phases and Lo/MedHi! This is unsupported! Defaulting to Phases.");
				if(!Los.Any() && (Phases.Any() || PhaseTransitions.Any()))
					PluginMain.DebugLog.LogError($"{manifest.Guid}:{sequence} does not contain a Lo nor any phases! This is unsupported!");

				data.Intro = Intros.ToArray();
				data.Lo = Los.ToArray();
				data.Transition = Transitions.ToArray();
				data.MedHi = MedHis.ToArray();
				data.End = Ends.ToArray();
				data.EndFail = EndFails.ToArray();
				data.OrbActivate = OrbActivates.ToArray();
				data.OrbHoldWave = OrbWave.ToArray();
				data.OrbSuccess = OrbSuccess.ToArray();
				data.OrbFailure = OrbFailure.ToArray();
				data.Phase = Phases;
				data.PhaseTransition = PhaseTransitions;
				sequenceDatas.Add(data);
			}
			
			//ingest takes
			List<TakeData> takeDatas = new List<TakeData>();
			//Get all the files that match the glob format of a take file (take_[timing]_[name])
			//See _FORMAT.txt for more info.
			string[] takes = Directory.GetFiles(dirPath, "take_*_*.wav", SearchOption.TopDirectoryOnly);
			takes = takes.Concat(Directory.GetFiles(dirPath, "take_*_*.ogg", SearchOption.TopDirectoryOnly)).ToArray();
			foreach (var take in takes) {
				Debug.Log($"Ingesting takes {take}");
				if (File.Exists(take) && !take.Contains(".wav") && !take.Contains(".ogg")) //it was ingesting the fucking yaml :/
					continue;
				string[] metadata = Path.GetFileName(take).Split('_');
				TakeData data = new TakeData();
				data.Timing = metadata[1];
				if (metadata.Length == 4) {
					data.Track.metadata = metadata[2].Split('-');
					data.Name = metadata[3];
				}
				else { //metadata.Length == 3
					data.Track.metadata = new[] { "" };
					data.Name = metadata[2];
				}

				string ext = Path.GetExtension(take);
				if (ext == ".wav") {
					data.Track.clip = WavUtility.ToAudioClip(take);
					data.Track.format = "wav";
				}
				else if (ext == ".ogg") {
					data.Track.clip = LoadOgg(take);
					data.Track.format = "ogg";
				}
				else
					PluginMain.DebugLog.LogError($"{take} has an invalid extension! (Valid extensions: .ogg, .wav)");
				takeDatas.Add(data);
			}
			manifest.Holds = sequenceDatas.ToArray();
			manifest.Takes = takeDatas.ToArray();
			manifest.Loaded = true;
		}
		
		//Convert a Yamlfest into a Manifest. As Yamlfest doesnt contain its path, it gotta be manually added.
		public static SoundtrackManifest ToManifest(this SoundtrackYamlfest yamlfest, string path) {
			var manifest = new SoundtrackManifest();
			manifest.Name = yamlfest.Name;
			manifest.Guid = yamlfest.Guid;
			manifest.Path = path;
			manifest.Location = yamlfest.location;
			manifest.Loaded = false;
			return manifest;
		}
		
		//Loads new soundtrack to be ran.
		public static void LoadSoundtrack(int index) {
			//Flag the game that we're doing soundtrack. Unflagging is done in BankAPI.SwapBanks.
			PluginMain.IsSoundtrack.Value = true;
			SelectedSoundtrackIndex = index;
			PluginMain.LastLoadedSoundtrack.Value = Soundtracks[SelectedSoundtrackIndex].Guid;
		}

		//I am a big hater of DRY
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
				Thread.Sleep(10);
			}
			AudioClip clip = www.GetAudioClip(false);
			clip.name = Path.GetFileName(path);
			return clip;
		}
		public static AudioClip? GetSnippet(SoundtrackManifest manifest) {
			string pathOgg = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path), manifest.Location), "snippet.ogg");
			string pathWav = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path), manifest.Location), "snippet.wav");

			AudioClip? clip = null;
			
			if(File.Exists(pathOgg))
				clip = LoadOgg(pathOgg);
			else if (File.Exists(pathWav))
				clip = WavUtility.ToAudioClip(pathWav);
			return clip;
		}
	}
}