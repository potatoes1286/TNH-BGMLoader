using System;
using System.IO;
using System.Linq;
using FMOD;
using FMODUnity;
using HarmonyLib;
using TNH_BGLoader;
using TNHBGLoader.Soundtrack;
using UnityEngine;

namespace TNHBGLoader
{
	public class Patcher_FMOD
	{
		[HarmonyPatch(typeof(RuntimeUtils), "GetBankPath")]
		[HarmonyPrefix]
		public static bool FMODRuntimeUtilsPatch_GetBankPath(ref string bankName, ref string __result)
		{
			// 100% going to fucking strangle the person who didn't perchance even fucking THINk that
			// sOMEONE WOULD INSERT A FUCKIGN ABSOLUTE PATH LOCATION. HOW STUPID ARE YOU???
			// "waa waa the dev will only want banks to be loaded from streamingassets"
			// ARE YOU FIFTH GRADE??? OF COURSE THERE'S GONNA BE AN EDGE CASE HAVE YOU NEVER PROGRAMMED BEFORE??
			// for a tad bit more context, i had to add the "ispathrooted" bit; fmod would force on the SA path
			
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (Path.GetExtension(bankName) != ".bank")
			{
				if (Path.IsPathRooted(bankName)){
					__result = bankName + ".bank";
					return false;
				} else {
					__result = string.Format("{0}/{1}.bank", streamingAssetsPath, bankName);
					return false;
				}
			}
			
			if (Path.IsPathRooted(bankName)) {
				__result = bankName;
				return false;
			} else {
				__result = string.Format("{0}/{1}", streamingAssetsPath, bankName);
				return false;
			}
		}
		
		[HarmonyPatch(typeof(RuntimeManager))]
		[HarmonyPatch("LoadBank", new Type[] { typeof(string), typeof(bool) })]
		[HarmonyPrefix]
		public static bool FMODRuntimeManagerPatch_LoadBank(ref string bankName)
		{
			if (bankName == "MX_TAH")
			{
				if (!BankAPI.BanksEmptyOrNull) //i don't even think this is possible? it's not. i need to remove this sometime.
				{
					/*if (BankAPI.CurrentBankLocation != "Select Random" && BankAPI.CurrentBankLocation != "Your Mix") {
						SoundtrackAPI.IsMix = false;
						PluginMain.IsSoundtrack.Value = false;
					}*/
					
					
					//this relies on Select Random being first. don't fucking touch it
					//And that Your Mix is second
					//The second check here (Your Mix + Soundtrack.Count == 0) is so that if there isnt any soundtracks, itll just act as Select Random.
					if (BankAPI.CurrentBankLocation == "Select Random" && PluginMain.IsSoundtrack.Value == false/* || (BankAPI.CurrentBankLocation == "Your Mix" && SoundtrackAPI.Soundtracks.Count == 0)*/) {
						PluginMain.DebugLog.LogInfo($"Activated Random/YourMix Count0. Current Bank: {BankAPI.CurrentBankLocation}");
						int num = UnityEngine.Random.Range(1, BankAPI.LoadedBankLocations.Count + SoundtrackAPI.Soundtracks.Count);
						PluginMain.DebugLog.LogInfo($"Selected: {num}");
						if (num < BankAPI.LoadedBankLocations.Count) {
							PluginMain.IsSoundtrack.Value = false;
							BankAPI.CurrentBankIndex = num;
						}
						else {
							PluginMain.IsSoundtrack.Value = true;
							SoundtrackAPI.SelectedSoundtrackIndex = num - BankAPI.LoadedBankLocations.Count;
						}
					}

					/*if (BankAPI.CurrentBankLocation == "Your Mix" && SoundtrackAPI.Soundtracks.Count != 0) {
						PluginMain.DebugLog.LogInfo($"Activated Your Mix");
						PluginMain.IsSoundtrack.Value = true;
						return false;
					}*/
					
					PluginMain.DebugLog.LogInfo($"IsSoundtrack loading bank/soundtrack time: {PluginMain.IsSoundtrack.Value.ToString()}");
					if (!PluginMain.IsSoundtrack.Value) {
						PluginMain.DebugLog.LogInfo("Injecting bank " + Path.GetFileName(BankAPI.CurrentBankLocation) +
						                   " into TNH!");
						bankName = BankAPI.CurrentBankLocation;
					}
					else {
						PluginMain
.DebugLog.LogInfo($"Loading soundtrack {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex].Guid} into TNH!");
					}

				}
			}
			return true;
		}
	}
}