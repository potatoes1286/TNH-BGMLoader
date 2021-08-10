using HarmonyLib;

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
	}
}