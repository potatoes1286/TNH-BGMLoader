using System.ComponentModel;
using System.Runtime.Serialization;
using FistVR;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace TNHBGLoader {
	public class AnnouncerYamlfest {
		public string Name { get; set; }

		[YamlMember(Alias = "Guid")] public string GUID { get; set; }

		public string VoiceLines { get; set; }

		[CanBeNull] public string Location { get; set; }

		[DefaultValue(0.2f)] public float FrontPadding { get; set; }

		[DefaultValue(1.2f)] public float BackPadding { get; set; }
	}
}