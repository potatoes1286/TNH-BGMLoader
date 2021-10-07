using FistVR;
using System;
using System.IO;
using FMOD;
using HarmonyLib;
using UnityEngine;
using Debug = FMOD.Debug;
using Random = UnityEngine.Random;
using System = FMOD.Studio.System;

namespace TNHBGLoader
{
	public class Patcher_FistVR
	{
		[HarmonyPatch(typeof(FVRFMODController), "SetMasterVolume")]
		[HarmonyPrefix]
		public static bool FVRFMODControllerPatch_SetMasterVolume(ref float i)
		{
			i *= TNHBackgroundMusicLoader.BackgroundMusicVolume.Value;
			return true;
		}
		
		[HarmonyPatch(typeof(TNH_UIManager), "Start")]
		[HarmonyPostfix]
		public static void TNH_UIManagerPatch_SpawnPanel()
		{
			TNHBackgroundMusicLoaderPanel BGMpanel = new TNHBackgroundMusicLoaderPanel();
			GameObject panel = BGMpanel.Panel.GetOrCreatePanel();
			panel.transform.position = new Vector3(0.0561f, 1f, 7.1821f);
			panel.transform.localEulerAngles = new Vector3(315, 0, 0);
			panel.GetComponent<FVRPhysicalObject>().SetIsKinematicLocked(true);
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