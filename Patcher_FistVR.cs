using FistVR;
using System;
using System.IO;
using FMOD;
using HarmonyLib;
using UnityEngine;
using Debug = FMOD.Debug;
using Random = UnityEngine.Random;
using System = FMOD.Studio.System;

namespace TNH_BGLoader
{
	public class Patcher_FistVR
	{
		[HarmonyPatch(typeof(FVRFMODController), "SetMasterVolume")]
		[HarmonyPrefix]
		public static bool FVRFMODControllerPatch_SetMasterVolume(ref float i)
		{
			i *= TNH_BGM_L.bgmVolume.Value;
			return true;
		}

		/*[HarmonyPatch(typeof(TNH_Manager), "Start")]
		[HarmonyPrefix]
		public static bool TNH_ManagerPatch_Start()
		{
			if (TNH_BGM_L.relevantBank == "Surprise Me!")
			{
				int bankNum = Random.Range(0, TNH_BGM_L.banks.Count);
				TNH_BGM_L.SwapBank(bankNum);
				TNH_BGM_L.relevantBank = "Surprise Me!";
				TNH_BGM_L.lastLoadedBank.Value = "Surprise Me!";
			}
			return true;
		}*/
	}
}