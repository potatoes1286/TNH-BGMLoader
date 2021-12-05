using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using FMODUnity;
using TNHBGLoader;
using UnityEngine;

namespace TNH_BGLoader
{
	public class BankAPI
	{
		
		public static List<string> BankList = new List<string>();
		public static int BankIndex = 0;
		private static readonly string PLUGINS_DIR = Paths.PluginPath;
		public static string loadedBank => BankList[BankIndex];
		public static bool BanksEmptyOrNull => (BankList == null || BankList.Count == 0);
		
		public static List<string> LegacyBanks
		{
			get
			{
				// surely this won't throw an access error!
				var banks = Directory.GetFiles(PLUGINS_DIR, "MX_TAH_*.bank", SearchOption.AllDirectories).ToList();
				// removes all files with parent dir "resources"
				foreach (var bank in banks) if (Path.GetFileName(Path.GetDirectoryName(bank))?.ToLower() == "resources") BankList.Remove(bank);
				Debug.Log(banks.Count + " banks loaded via legacy bank loader! - PTNHBGML");
				// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
				return banks;
			}
		}
		
		public delegate void bevent();
		public static event bevent OnBankSwapped;
		public static void SwapBank(int newBank)
		{
			//wrap around
			newBank = Mathf.Clamp(newBank, 0, BankList.Count - 1);
			UnloadBankHard(loadedBank); //force it to be unloaded
			BankIndex = newBank; //set banknum to new bank
			NukeSongSnippets();
			RuntimeManager.LoadBank("MX_TAH"); //load new bank (MX_TAH sets off the patcher)
			PluginMain.LastLoadedBank.Value = Path.GetFileNameWithoutExtension(loadedBank); //set last loaded bank
		}

		public static void NukeSongSnippets()
		{
			if (OnBankSwapped != null) OnBankSwapped(); //null moment!
		}
		
		//literal copy of RuntimeManager.UnloadBank but hard unloads
		public static void UnloadBankHard(string bankName)
		{
			UnityEngine.Debug.Log("Hard unloading " + Path.GetFileName(bankName));
			RuntimeManager.LoadedBank value;
			if (RuntimeManager.Instance.loadedBanks.TryGetValue(bankName, out value))
			{
				value.RefCount = 0;
				value.Bank.unload();
				RuntimeManager.Instance.loadedBanks.Remove(bankName);
			}
		}
		
		//please co-routine this. doing this on the main thread is just asking for a freeze
		//granted it's 256x256 (usually), how hard can it be to load that?
		public static Texture2D LoadIconForBank(string bankName)
		{
			Debug.Log("Loading image for " + bankName);
			var pbase = Path.GetDirectoryName(bankName) + "/";
			var name = Path.GetFileNameWithoutExtension(bankName).Split('_').Last();
			string[] paths = new string[]
			{
				pbase + name + ".png",
				Directory.GetParent(pbase) + name + ".png",
				pbase + "iconhq.png",
				Directory.GetParent(pbase) + "iconhq.png",
				pbase + "icon.png",
				Directory.GetParent(pbase) + "icon.png"
			};
			if (bankName == Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"))
			{
				paths = new string[]
				{
					PluginMain.AssemblyDirectory + "/defaulticonhq.png"
				};
			}
			
			foreach(var path in paths)
			{
				if (File.Exists(path))
				{
					Debug.Log("Loading from " + path);
					//var tex = new WWW("file:///" + pbase + "iconhq.png").texture;
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1,1);
					tex.LoadImage(byteArray);
					if (tex != null)
					{
						Debug.Log("Loaded fine!");
						return tex;
					}
					else Debug.Log("Failed lo load!");
				} else Debug.Log(path + " does not exist!");
			}
			return null;
		}
	}
}