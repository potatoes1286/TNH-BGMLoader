using System.Collections.Generic;
using System.Linq;
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
			var speechSet = Resources.LoadAll<SosigSpeechSet>("").First(x => x.name == "SosigSpeech_Anton");
			speechSet.BasePitch = 1.15f;
			return new SosigManifest()
				   {
					   name = "Default",
					   guid = "h3vr.default",
					   SpeechSet = speechSet
				   };
		}
		public static SosigManifest DefaultZosigVLS()
		{
			var speechSet = Resources.LoadAll<SosigSpeechSet>("").First(x => x.name == "SosigSpeech_Zosig");
			speechSet.BasePitch = 1.15f;
			return new SosigManifest()
			{
				name = "Zosig",
				guid = "h3vr.zosig",
				SpeechSet = speechSet
			};
		}
		
		public static SosigManifest RandomSosigVLS()
		{
			var speechSet = ScriptableObject.CreateInstance<SosigSpeechSet>();
			speechSet.BasePitch = 1.15f;
			return new SosigManifest()
				   {
					   name = "Select Random Voiceline",
					   guid = "ptnhbgml.random",
					   SpeechSet = speechSet
				   };
		}
	}
}