using System;
using System.Collections;
using TNHBGLoader;
using UnityEngine;
using UnityEngine.UI;

namespace TNH_BGLoader
{
	public class IconDisplayWaitForInit : MonoBehaviour
	{
		public TNHPanel bgmpanel;
		public GameObject panel;
		public int attempts = 500;
		private RawImage rawimagecomp;
		public void Start()
		{
			gameObject.name = "Icon Displayer Thingamajig";
			rawimagecomp = gameObject.AddComponent<RawImage>();
		}
		
		public void FixedUpdate()
		{
			try
			{
				gameObject.transform.parent = panel.transform.GetChild(2).GetChild(0); //canvas of the panel
				gameObject.transform.localPosition = new Vector3(-28, 0, 0);
				gameObject.transform.localRotation = Quaternion.identity;
				gameObject.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
				bgmpanel.icondisplay = rawimagecomp;
				bgmpanel.SetCorruptButtonToShow(false);
				rawimagecomp.texture = BankAPI.LoadIconForBank(BankAPI.LoadedBankLocation);
				Destroy(this);
			}
			catch
			{
				if(attempts == 0) Destroy(this);
				attempts--;
				//Debug.Log(attempts + "attempts left!");
			}
		}
	}
}