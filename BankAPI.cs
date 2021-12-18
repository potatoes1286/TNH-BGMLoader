using System;
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
		//Bank Index, Bank Name, Bank Location
		public static List<string> BankLocations = new List<string>();
		public static int LoadedBankIndex = 0;
		public static string LoadedBankLocation => BankLocations[LoadedBankIndex];
		public static bool BanksEmptyOrNull => (BankLocations == null || BankLocations.Count == 0);
		public static List<string> LegacyBanks
		{
			get
			{
				// surely this won't throw an access error!
				var banks = Directory.GetFiles(Paths.PluginPath, "MX_TAH_*.bank", SearchOption.AllDirectories).ToList();
				// removes all files with parent dir "resources"
				foreach (var bank in banks) if (Path.GetFileName(Path.GetDirectoryName(bank))?.ToLower() == "resources") BankLocations.Remove(bank);
				PluginMain.DebugLog.LogDebug(banks.Count + " banks loaded via legacy bank loader!");
				// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
				return banks;
			}
		}
		public static string BankLocationToName(string loc) => Path.GetFileNameWithoutExtension(loc).Split('_').Last();
		public static string BankIndexToName(int index, bool returnWithIndex = false)
		{
			string bankpath = BankAPI.BankLocations[index];
			string bankname = Path.GetFileNameWithoutExtension(bankpath).Split('_').Last();
			if (bankname == "TAH") bankname = "Default";
			if (returnWithIndex) bankname = (index + 1) + ": " + bankname;
			return bankname;
		}
		public delegate void bevent(); public static event bevent OnBankSwapped;
		public static void SwapBank(int newBank)
		{
			//wrap around
			newBank = Mathf.Clamp(newBank, 0, BankLocations.Count - 1);
			UnloadBankHard(LoadedBankLocation); //force it to be unloaded
			LoadedBankIndex = newBank; //set banknum to new bank
			NukeSongSnippets();
			RuntimeManager.LoadBank("MX_TAH"); //load new bank (MX_TAH sets off the patcher)
			PluginMain.LastLoadedBank.Value = Path.GetFileNameWithoutExtension(LoadedBankLocation); //set last loaded bank
		}
		public static void NukeSongSnippets()
		{
			if (OnBankSwapped != null) OnBankSwapped(); //null moment!
		}
		//literal copy of RuntimeManager.UnloadBank but hard unloads
		public static void UnloadBankHard(string bankName)
		{
			PluginMain.LogSpam("Hard unloading " + Path.GetFileName(bankName));
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
			PluginMain.LogSpam("Loading image for " + bankName);
			//get the name and base path of the bank
			var pbase = Path.GetDirectoryName(bankName) + "/";
			var name = Path.GetFileNameWithoutExtension(bankName).Split('_').Last();
			//assembles all the potential locations for the icon, in descending order of importance.
			string[] paths = new string[] //this is fucking terrible. less so than the announcer one tho
			{
				pbase + name + ".png",
				Directory.GetParent(pbase) + name + ".png",
				pbase + "iconhq.png",
				Directory.GetParent(pbase) + "iconhq.png",
				pbase + "icon.png",
				Directory.GetParent(pbase) + "icon.png"
			};
			//get default bank loc
			if (bankName == Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank"))
				paths = new string[] {PluginMain.AssemblyDirectory + "/default/bank_default.png"};

			//iterate through all paths, get the first one that exists
			foreach(var path in paths)
			{
				if (File.Exists(path))
				{
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1,1);
					tex.LoadImage(byteArray);
					if (tex != null)
					{
						PluginMain.LogSpam("Loading icon from " + path);
						return tex;
					}
				}
			}
			PluginMain.DebugLog.LogError("Cannot find icon for " + BankAPI.BankLocationToName(bankName) + "!\nPossible locations:\n" + String.Join("\n", paths));
			return null;
		}
	}
}