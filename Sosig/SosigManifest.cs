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
		public List<string> previews;
		
		public static readonly SosigManifest DefaultSosigVLS = new SosigManifest()
		{
			name = "Default",
			guid = "h3vr.default"
		};
	}
}