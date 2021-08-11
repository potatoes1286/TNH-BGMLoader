﻿using System;
using System.IO;
using FMODUnity;
using HarmonyLib;
using UnityEngine;

namespace TNH_BGLoader
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
		[HarmonyPatch("LoadBank", new Type[]{typeof(string), typeof(bool)})]
		[HarmonyPrefix]
		public static bool FMODRuntimeManagerPatch_LoadBank(ref string bankName)
		{
			if (bankName == "MX_TAH")
			{
				if (!TNH_BGM_L.areBanksEmptyOrNull)
				{
					Debug.Log("Injecting bank " + Path.GetFileName(TNH_BGM_L.relevantBank) + " into TNH!");
					bankName = TNH_BGM_L.relevantBank;
				}
			}
			return true;
		}
	}
}