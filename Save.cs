using System.IO;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TNHBGLoader
{
	public class Save
	{
		public string   LastLoadedBank      { get; set; }
		public string   LastLoadedAnnouncer { get; set; }
		public string[] LastLoadedVls       { get; set; }
		public string[] LastLoadedVlsSets   { get; set; }

		public void Write()
		{
			string save = Path.Combine(Directory.GetCurrentDirectory(), "lastloaded.yaml");
			var _serializer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
			var yamlfest = _serializer.Serialize(this);
			File.WriteAllText(yamlfest, save);
		}

		public static Save Read()
		{
			string save = Path.Combine(Directory.GetCurrentDirectory(), "lastloaded.yaml");
			var _deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
			var yamlfest = _deserializer.Deserialize<Save>(File.ReadAllText(save));
			return yamlfest;
		}
		
	}
}