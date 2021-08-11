using System.Collections.Generic;
using System.IO;
using YamlDotNet;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TNH_BGLoader
{
	//i've been informed this is a terrible idea
	//unfortunately, if something being a terrible idea stopped me i wouldnt be here
	//that being said, this is also temporary until R2MM fixes bank disabling
	public class YAMLparser
	{
		public static void LoadYAMLModData()
		{
			string yaml = File.ReadAllText(GetModsYMLfilePath());
			var yamldec = DeserializeModsYML(yaml);
		}

		public static string GetModsYMLfilePath()
		{
			string path = BepInEx.Paths.BepInExRootPath + "/Mods.yml";
			if (Directory.Exists(path)) return path;
			return null;
		}

		public static List<ModsYaml_Strut> DeserializeModsYML(string yamlfile)
		{
			var deserializer = new DeserializerBuilder().Build();
			return deserializer.Deserialize<List<ModsYaml_Strut>>(yamlfile);
		}
	}

	public class ModsYaml_Strut
	{
		public string name { get; set; }
		public bool enabled { get; set; }
	}
}