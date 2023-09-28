using System;
using FistVR;
using FMOD.Studio;
using FMODUnity;
using TNHBGLoader;
using TNHBGLoader.Soundtrack;
using UnityEngine;

namespace TNH_BGLoader
{
	//Plays a snipped of a song.
	public class PlaySoundtrackSnippet : MonoBehaviour {

		public static PlaySoundtrackSnippet? existingSnippet = null;
		
		public AudioSource source;
		public float       maxVol       = 0.5f;
		public float       maxVolLength = 6f;

		public bool IsLoop;

		public bool playing = false;
		
		//time to get a sin wave from 0 to 1
		public float windUpTime = 2f;
		public float curLength;
		public float curVol;
		// sin wave from 0-1, defined by the windup time -> play at max vol for maxvollength -> sin wave from 1-0

		public void CleanUp() {
			existingSnippet = null;
			Destroy(source);
			Destroy(gameObject);
		}
		
		public void Start() {
			if(existingSnippet != null)
				existingSnippet.CleanUp();
			existingSnippet = this;
			source = GM.Instance.m_currentPlayerBody.Head.gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.priority = 0;
			source.volume = curVol;
			source.spatialBlend = 0;
			source.loop = true; //lets just fucking assume huh

			var tuple = SoundtrackAPI.GetSnippet(SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrackIndex]);
			var clip = tuple.Item1;
			IsLoop = tuple.Item2;

			if (clip == null)
				CleanUp();
			else {
				source.clip = clip;
				maxVolLength = clip.length - 2 * windUpTime;
				source.loop = true;
				source.Play();
				playing = true;
			}
		}

		public void Update() {
			if (!playing)
				return;
			curLength += Time.deltaTime; //tick forwards the time
			if(curLength > windUpTime && IsLoop) //stop if its looping
				return;
			
			curVol = GetVol(); //get the volume
			source.volume = curVol * 0.25f * PluginMain.BackgroundMusicVolume.Value; //set the volume
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