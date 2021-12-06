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
		[CanBeNull]
		public TNH_VoiceLineID ID { get; set; }
		[CanBeNull]
		public string StandardAudioClipPath { get; set; }
		[CanBeNull]
		public string CorruptedAudioClipPath { get; set; }
	}

	public class AnnouncerManifest
	{
		[CanBeNull]
		public string Location { get; set; }
		[CanBeNull]
		public string Icon { get; set; }
		public string Name { get; set; }
		
		[YamlMember(Alias = "Guid")]
		public string GUID { get; set; }
		public VoiceLine[] VoiceLines { get; set; }
		[CanBeNull]
		[YamlMember(Alias = "hascorruptedver")]
		public bool HasCorruptedVer { get; set; }

		public static readonly AnnouncerManifest DefaultAnnouncer = new AnnouncerManifest()
		{
			Name = "Default TNH Announcer",
			GUID = "h3vr.default",
			HasCorruptedVer = true,
			VoiceLines = new VoiceLine[0],
			Location = PluginMain.AssemblyDirectory + "/defaultannouncericonhq.png"
		};
	}
}