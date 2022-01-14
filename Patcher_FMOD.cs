using System;
using System.IO;
using FMODUnity;
using HarmonyLib;
using TNH_BGLoader;
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
					//this relies on Select Random being first. don't fucking touch it
					if (BankAPI.CurrentBankLocation == "Select Random") BankAPI.CurrentBankIndex = UnityEngine.Random.Range(1, BankAPI.LoadedBankLocations.Count);
					PluginMain.LogSpam("Injecting bank " + Path.GetFileName(BankAPI.CurrentBankLocation) + " into TNH!");
					bankName = BankAPI.CurrentBankLocation;
				}
			}
			return true;
		}
	}
}