using YamlDotNet.Serialization;


namespace TNHBGLoader.Soundtrack {
	public class SoundtrackYamlfest {
		public string Name { get; set; }
		//GUID should be written in form "author.name".
		//GUID should not be changed, ever
		public string Guid { get; set; }
		//I'm not sure why this was here? My test mod had simply written "
		//soundtrack: soundtrack"
		//Seems useless. Surely wont bite me back in the future. I'll just doink it.
		//Update. It bit me. I'm renaming it to location.
		public string Location { get; set; }
		public string GameMode { get; set; }
	}
}