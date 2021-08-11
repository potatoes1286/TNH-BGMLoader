using FistVR;
using FMOD;
using HarmonyLib;
using UnityEngine;
using Debug = FMOD.Debug;

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
			int nbn = TNH_BGM_L.bankNum + 1;
			if (nbn > TNH_BGM_L.banks.Count) nbn = 0; //wrap around
			TNH_BGM_L.SwapBanks(nbn);
			return true;
		}*/
	}
}