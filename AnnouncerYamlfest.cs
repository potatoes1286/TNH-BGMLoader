using FistVR;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace TNHBGLoader
{
	public class AnnouncerYamlfest
	{
		public string Name { get; set; }
		[YamlMember(Alias = "Guid")] public string GUID { get; set; }
		public string VoiceLines { get; set; }
		[CanBeNull] public string Location { get; set; }
	}
}