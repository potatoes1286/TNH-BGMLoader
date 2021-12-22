using System;
using FMOD.Studio;
using FMODUnity;
using TNHBGLoader;
using UnityEngine;

namespace TNH_BGLoader
{
	public class PlaySongSnippet : MonoBehaviour
	{
		public EventInstance snippet;
		public Bus mbus;
		public float maxVol = 0.5f;
		public float maxVolLength = 6f;
		public float audoffset = 44f;
		
		//time to get a sin wave from 0 to 1
		public float windUpTime = 2f;
		public float curLength;
		public float curVol;
		// sin wave from 0-1, defined by the windup time -> play at max vol for maxvollength -> sin wave from 1-0

		public void CleanUp()
		{
			BankAPI.OnBankSwapped -= OnBankChanged;
			snippet.stop(STOP_MODE.IMMEDIATE);
			snippet.release();
			Destroy(gameObject);
		}
		
		//bank changed, clean tf up
		public void OnBankChanged() { CleanUp(); }
		
		public void Start()
		{
			PluginMain.LogSpam("Playing snippet " + BankAPI.GetNameFromIndex(BankAPI.CurrentBankIndex));
			mbus = RuntimeManager.GetBus("bus:/Music");
			mbus.setVolume(0.25f * PluginMain.BackgroundMusicVolume.Value);
			snippet = RuntimeManager.CreateInstance("event:/MX/TAH/Fake Meat Must Die");
			snippet.setTimelinePosition((int)(audoffset * 1000f));
			snippet.start();
			snippet.setParameterValue("Intensity", 2);
			snippet.setTimelinePosition((int)(audoffset * 1000f));
			BankAPI.OnBankSwapped += OnBankChanged;
		}

		public void Update()
		{
			mbus.setVolume(0.25f * PluginMain.BackgroundMusicVolume.Value);
			curLength += Time.deltaTime; //tick forwards the time
			curVol = GetVol(); //get the volume
			snippet.setVolume(curVol); //set the volume
			int pos = -6;
			snippet.getTimelinePosition(out pos);
			//Debug.Log(pos);
			if (curLength >= maxVolLength + (windUpTime * 2)) //if volume = 0; we finished. clean up!
				CleanUp();
		}

		public float GetVol()
		{
			if (curLength < windUpTime) //winding up
				return lerpvol(curLength, windUpTime, maxVol);
			if (curLength < windUpTime + maxVolLength) //max vol
				return maxVol;
			return lerpvol(curLength + windUpTime, windUpTime, maxVol); //wind down
		}
		
		public float lerpvol(float t, float Twindup, float Vmax) => (-Mathf.Cos(Mathf.PI * (t / Twindup)) + 1) / 2 * Vmax;
	}
}