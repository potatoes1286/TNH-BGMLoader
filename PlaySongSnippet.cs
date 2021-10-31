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
			PluginMain.OnBankSwapped -= OnBankChanged;
			snippet.stop(STOP_MODE.IMMEDIATE);
			snippet.release();
			Destroy(gameObject);
		}
		
		public void OnBankChanged() //bank changed, clean tf up
		{
			CleanUp();
		}
		
		public void Start()
		{
			mbus = RuntimeManager.GetBus("bus:/Music");
			mbus.setVolume(0.25f * PluginMain.BackgroundMusicVolume.Value);
			snippet = RuntimeManager.CreateInstance("event:/MX/TAH/Fake Meat Must Die");
			snippet.setTimelinePosition((int)(audoffset * 1000f));
			snippet.start();
			snippet.setParameterValue("Intensity", 2);
			snippet.setTimelinePosition((int)(audoffset * 1000f));
			PluginMain.OnBankSwapped += OnBankChanged;
		}

		public void Update()
		{
			mbus.setVolume(0.25f * PluginMain.BackgroundMusicVolume.Value);
			curLength += Time.deltaTime; //tick forwards the time
			curVol = GetVol(); //get the volume
			snippet.setVolume(curVol); //set the volume
			int pos = -6;
			snippet.getTimelinePosition(out pos);
			Debug.Log(pos);
			if (curLength >= maxVolLength + (windUpTime * 2)) //if volume = 0; we finished. clean up!
			{
				CleanUp();
			}
		}

		public float GetVol()
		{
			float vol = 0;
			if (curLength < windUpTime) //if winding up
			{
				return Mathf.Sin((curLength * (Mathf.PI / 2)) / windUpTime) * maxVol;
			}
			if (curLength < windUpTime + maxVolLength) //during maxvolplay time
			{
				return maxVol;
			}
			//if winding down
			return Mathf.Sin(((curLength + windUpTime) * (Mathf.PI / 2)) / windUpTime) * maxVol;
		}
	}
}