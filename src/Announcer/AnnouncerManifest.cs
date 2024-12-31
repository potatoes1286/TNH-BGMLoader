using System.Collections.Generic;
using System.ComponentModel;
using FistVR;
using JetBrains.Annotations;
using TNHBGLoader;
using UnityEngine.Internal;
using YamlDotNet;
using YamlDotNet.Serialization;

namespace TNH_BGLoader
{
	public class VoiceLine
	{
		[YamlMember(Alias = "id")]
		public TNH_VoiceLineID ID { get; set; }
		[CanBeNull]
		public string ClipPath { get; set; }
	}

	public class AnnouncerManifest
	{
		[CanBeNull] public string       Location { get; set; }
		[CanBeNull] public List<string> Previews { get; set; }
		public             string       Name     { get; set; }
		
		public string          GUID       { get; set; }
		public List<VoiceLine> VoiceLines { get; set; }

		public float FrontPadding { get; set; }
		public float BackPadding  { get; set; }

		public static readonly AnnouncerManifest DefaultAnnouncer =
			new AnnouncerManifest()
			{
				Name = "Default Announcer",
				GUID = "h3vr.default",
				VoiceLines = new List<VoiceLine>(),
				FrontPadding = 0.2f,
				BackPadding = 1.2f
			};
		
		public static readonly AnnouncerManifest RandomAnnouncer =
			new AnnouncerManifest()
			{
				Name = "Select Random Announcer",
				GUID = "ptnhbgml.random",
				VoiceLines = new List<VoiceLine>(),
				FrontPadding = 0.2f,
				BackPadding = 1.2f
			};

		public static readonly AnnouncerManifest CorruptedAnnouncer =
			new AnnouncerManifest()
			{
				Name = "Corrupted Announcer",
				GUID = "h3vr.corrupted",
				VoiceLines = new List<VoiceLine>(),
				FrontPadding = 0.2f,
				BackPadding = 1.2f
			};
	}
}