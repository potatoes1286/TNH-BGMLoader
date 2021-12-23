using System.Collections.Generic;
using FistVR;
using UnityEngine;

namespace TNHBGLoader.Sosig
{
	public class SosigManifest
	{
		public SosigSpeechSet SpeechSet;
		public string guid;
		public string name;
		public string location;
		public List<AudioClip> previews = new List<AudioClip>();
		
		public static SosigManifest DefaultSosigVLS()
		{
			var speechSet = ScriptableObject.CreateInstance<SosigSpeechSet>();
			speechSet.BasePitch = 1.15f;
			return new SosigManifest()
				   {
					   name = "Default",
					   guid = "h3vr.default",
					   SpeechSet = speechSet
				   };
		}
	}
}