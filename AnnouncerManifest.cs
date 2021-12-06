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
		[CanBeNull]
		public string Location { get; set; }
		[CanBeNull]
		public List<string> Previews { get; set; }
		public string Name { get; set; }
		
		[YamlMember(Alias = "Guid")]
		public string GUID { get; set; }
		public List<VoiceLine> VoiceLines { get; set; }
		[CanBeNull]

		public static readonly AnnouncerManifest DefaultAnnouncer = new AnnouncerManifest()
		{
			Name = "Default TNH Announcer",
			GUID = "h3vr.default",
			VoiceLines = new List<VoiceLine>(),
			Location = PluginMain.AssemblyDirectory + "/defaultannouncericonhq.png"
		};
	}
}