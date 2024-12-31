using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace TNHBGLoader {
	public static class Common {
		/// <summary>
		/// Wraps an int to a minimum and maximum, rolling over on overflow/underflow.
		/// </summary>
		/// <returns>The wrapped integer.</returns>
		/// <param name="min">Minimum before wrap. Inclusive.</param>
		/// <param name="max">Maximum before wrap. Inclusive.</param>>
		public static int Wrap(this int self, int min, int max) {
			if (self < min)
				return max - (self + min + 1);
			if (self > max)
				return min + (self - max - 1);
			return self;
		}

		/// <summary>
		/// Wraps an int to a minimum and maximum, rolling over on overflow/underflow.
		/// </summary>
		/// <returns>The wrapped integer.</returns>
		/// <param name="range">A set of two numbers of minimum and maximum. Both inclusive.</param>
		public static int Wrap(this int self, int[] range) {
			return self.Wrap(range[0], range[1]);
		}

		/// <summary>
		/// Attempts to load clip provided, supports ogg and wav.
		/// </summary>
		public static AudioClip LoadClip(string path, bool loadWav = true) {
			var ext = Path.GetExtension(path)!;
			if (ext == ".wav" && loadWav) {
				try {
					return WavUtility.ToAudioClip(path);
				}
				catch (Exception e) {
					PluginMain.DebugLog.LogError($"Failed loading audio file {Path.GetFileName(path)} with reason {e}");
					return null;
				}
			}

			if (ext == ".ogg") {
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

			if (loadWav)
				PluginMain.DebugLog.LogError("Failed loading audio file {Path.GetFileName(path)}" +
				                             "with reason unsupported extension; Supported values: .ogg, .wav");
			else
				PluginMain.DebugLog.LogError("Failed loading audio file {Path.GetFileName(path)}" +
				                             "with reason unsupported extension; Supported values: .ogg");
			return new AudioClip();
		}
	}
}