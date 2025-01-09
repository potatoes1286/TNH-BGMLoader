using System;
using System.IO;
using TNHBGLoader.Sosig;
using UnityEngine;

namespace TNHBGLoader
{
	public class GeneralAPI : MonoBehaviour
	{
		public static Texture2D GetIcon(string guid, string[] paths)
		{
			PluginMain.LogSpam("Loading image for " + guid);
			//iterate through all paths, get the first one that exists
			foreach (var path in paths) {
				if (File.Exists(path)) {
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1, 1);
					tex.LoadImage(byteArray);
					if (tex != null) {
						PluginMain.LogSpam("Loading icon from " + path);
						return tex;
					}
				}
			}
			PluginMain.DebugLog.LogError("Cannot find icon for " + guid + "!\nPossible locations:\n" + String.Join("\n", paths));
			return null;
		}

		public static bool IfIsInRange(float input, float min, float max)
		{
			//inelegant but short way. fuck you
			return Mathf.Clamp(input, min, max) == input; //gee i sure do hope float rounding doesn't fuck me over
		}
	}
}