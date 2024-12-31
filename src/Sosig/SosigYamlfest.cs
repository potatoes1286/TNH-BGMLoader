using FistVR;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace TNHBGLoader
{
	public class SosigYamlfest
	{
		public    string  Name                     { get; set; }
		[YamlMember(Alias = "Guid")] 
		public    string  GUID                     { get; set; }

		public string  VoiceLines               { get; set; }
		public string? Location                 { get; set; }
		public float   BasePitch                { get; set; }
		public float   BaseVolume               { get; set; }
		public bool    ForceDeathSpeech         { get; set; }
		public bool    UseAltDeathOnHeadExplode { get; set; }
		public bool    LessTalkativeSkirmish    { get; set; }
	}
}