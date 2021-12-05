using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FistVR;
using TNHBGLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace TNH_BGLoader
{
	public class AnnouncerAPI
	{
		//Announcer Manifests and Announcer Index - USE MANIFESTS!
		public static List<AnnouncerManifest> Announcers = new List<AnnouncerManifest>();
		public static int LoadedAnnouncerIndex = 0;
		public static AnnouncerManifest CurrentAnnouncer => Announcers[LoadedAnnouncerIndex];

		public static void SwapAnnouncer(AnnouncerManifest announcer)
		{
			LoadedAnnouncerIndex = GetAnnouncerIndex(announcer);
		}

		public static int GetAnnouncerIndex(AnnouncerManifest announcer) => Announcers.IndexOf(announcer);

		//I should probably also co-routine this, but co-routine throws a hissyfit whenever i do for some reason.
		public static Texture2D GetAnnouncerTexture(AnnouncerManifest announcer)
		{
			//Debug.Log("Loading image for " + bankName);
			var pbase = Path.GetDirectoryName(announcer.Location);
			//assembles all the potential locations for the icon, in descending order of importance.
			string[] paths = new string[] //this is fucking terrible.
			{
				//announcer-specific icon
				pbase + "/" + announcer.GUID + ".png",
				//hq icon for all announcers- should be based of TS image
				pbase + "/iconhq.png",
				Directory.GetParent(pbase) + "/iconhq.png",
				Directory.GetParent(Directory.GetParent(pbase).ToString()) + "/iconhq.png",
				//TS images
				pbase + "/icon.png",
				Directory.GetParent(pbase) + "/icon.png",
				Directory.GetParent(Directory.GetParent(pbase).ToString()) + "/icon.png",
			};
			if (announcer.GUID == "h3vr.default")
				paths = new string[] {PluginMain.AssemblyDirectory + "/defaultannouncericonhq.png"};
			if (!string.IsNullOrEmpty(announcer.Icon))
				paths = new string[] {announcer.Icon};

			//iterate through all paths, get the first one that exists
			foreach (var path in paths)
			{
				if (File.Exists(path))
				{
					//Debug.Log("Loading from " + path);
					//var tex = new WWW("file:///" + pbase + "iconhq.png").texture;
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1, 1);
					tex.LoadImage(byteArray);
					if (tex != null)
					{
						//Debug.Log("Loaded fine!");
						return tex;
					}
					//else Debug.Log("Failed lo load!");
				} //else Debug.Log(path + " does not exist!");
			}
			Debug.LogWarning("Cannot find icon for " + announcer.GUID + "!");
			return null;
		}

		/*public static IEnumerator AddVoiceLinesToDB(ref TNH_VoiceDatabase db, VoiceLine line)
		{
			//i know there's a special place in hell for my naming scheme. dont care
			var sawww = GetAudioFromFile(line.StandardAudioClipPath);
			var cawww = GetAudioFromFile(line.CorruptedAudioClipPath);
			yield return sawww;
			yield return cawww;
			var sa = sawww.GetAudioClip();
			var ca = cawww.GetAudioClip();
			
			var vl = new TNH_VoiceDatabase.TNH_VoiceLine();
			vl.ID = line.ID;
			vl.Clip_Standard = sa;
			vl.Clip_Corrupted = ca;
			db.Lines.Add(vl);
		}*/
		
		//why does this field even exist lol
		public static AudioClip GetAudioFromFile(string path) => WavUtility.ToAudioClip(path);
	}
}