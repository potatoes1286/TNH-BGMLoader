using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using FMODUnity;
using TNHBGLoader;
using TNHBGLoader.Soundtrack;
using UnityEngine;

namespace TNH_BGLoader
{
	public class BankAPI
	{
		//Bank Index, Bank Name, Bank Location
		//Standard: Bank Index
		public static List<string> LoadedBankLocations = new List<string>();
		public static int CurrentBankIndex = 0;
		public static string CurrentBankLocation => LoadedBankLocations[CurrentBankIndex];
		//i don't actually think this will ever return true. TODO: check
		public static bool BanksEmptyOrNull => (LoadedBankLocations == null || LoadedBankLocations.Count == 0);
		public static List<string> GetLegacyBanks() {
			List<string> banks = new List<string>();
			banks.Add(Path.Combine(Application.streamingAssetsPath, "MX_TAH.bank")); //the loader patch just checks for MX_TAH, not the full root path so this bypasses the check
			// surely this won't throw an access error!
			banks = banks.Concat(Directory.GetFiles(Paths.PluginPath, "MX_TAH_*.bank", SearchOption.AllDirectories).ToList()).ToList();
			// removes all files with parent dir "resources"
			foreach (var bank in banks)
				if (Path.GetFileName(Path.GetDirectoryName(bank))?.ToLower() == "resources")
					LoadedBankLocations.Remove(bank);
			PluginMain.DebugLog.LogInfo(banks.Count + " banks loaded via legacy bank loader!");
			// i'm supposed to ignore any files thrown into the plugin folder, but idk how to do that. toodles!
			return banks;
		}
		public static string GetNameFromLocation(string loc) => Path.GetFileNameWithoutExtension(loc).Split('_').Last();
		public static string GetNameFromIndex(int index, bool returnWithIndex = false)
		{
			string bankpath = BankAPI.LoadedBankLocations[index];
			string bankname = Path.GetFileNameWithoutExtension(bankpath).Split('_').Last();
			if (bankname == "TAH") bankname = "Default";
			if (returnWithIndex) bankname = (index + 1) + ": " + bankname;
			return bankname;
		}
		public delegate void bevent(); public static event bevent OnBankSwapped;
		public static void SwapBank(int newBank)
		{
			//If over the # of banks, overflow into Soundtrack API.
			if (newBank > LoadedBankLocations.Count - 1) {
				//Shit over the workload to the soundtrack API.
				//newBank - LoadedBankLocations.Count results in the # as it corresponds to in the soundtrack data
				//Ex: If you have 5 Banks, and you ask for Index 5 (6th bank), it corresponds to Index 0 (1st soundtrack)
				//Yeah, its hacky. But the man who plans still hasn't started and my code works.
				SoundtrackAPI.LoadSoundtrack(newBank - LoadedBankLocations.Count);
			} else { //If not overflow, do outdated Bank method.
				//Ensure the game doesn't think we're doing soundtrack method.
				//Flagging is done in SoundtrackAPI.LoadSoundtrack.
				PluginMain.IsSoundtrack.Value = false;
				UnloadBankHard(CurrentBankLocation); //force it to be unloaded
				CurrentBankIndex = newBank; //set banknum to new bank
				NukeSongSnippets();
				//load new bank (MX_TAH sets off the patcher Patcher_FMOD.FMODRuntimeManagerPatch_LoadBank)
				RuntimeManager.LoadBank("MX_TAH"); 
				PluginMain.LastLoadedBank.Value =
					Path.GetFileNameWithoutExtension(CurrentBankLocation); //set last loaded bank
			}
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
		public static Texture2D GetBankIcon(string bankName)
		{
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

			return GeneralAPI.GetIcon(bankName, paths);
		}
	}
}