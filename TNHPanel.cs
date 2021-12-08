using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FistVR;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using Sodalite;
using Sodalite.Api;
using Sodalite.UiWidgets;
using Sodalite.Utilities;
using TNH_BGLoader;
using Valve.Newtonsoft.Json.Utilities;

namespace TNHBGLoader
{
	public class TNHPanel : MonoBehaviour
	{
		public LockablePanel Panel;
		public enum TNHPstates { BGM, Announcer }
		public TNHPstates TNHPstate = TNHPstates.BGM;

		public TNHPanel()
		{
			Panel = new LockablePanel();
			Panel.Configure += ConfigurePanel;
			Panel.TextureOverride = SodaliteUtils.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("panel.png"));
		}
		
		private TextWidget   _bankText;
		private TextWidget   _volumeText;

		private ButtonWidget[] _musicButtons = new ButtonWidget[8];
		private ButtonWidget[] _volControls = new ButtonWidget[2];
		private ButtonWidget[] _cycleControls = new ButtonWidget[2];
		private ButtonWidget _switchstate;

		private int _firstMusicIndex;

		public RawImage icondisplay;

		private void ConfigurePanel(GameObject panel)
		{
			GameObject canvas = panel.transform.Find("OptionsCanvas_0_Main/Canvas").gameObject;
			UiWidget.CreateAndConfigureWidget(canvas, (GridLayoutWidget widget) =>
			{
				#region Initialize Panel
				// Fill our parent and set pivot to top middle
				widget.RectTransform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
				widget.RectTransform.localPosition = Vector3.zero;
				widget.RectTransform.anchoredPosition = Vector2.zero;
				widget.RectTransform.sizeDelta = new Vector2(37f / 0.07f, 24f / 0.07f);
				widget.RectTransform.pivot = new Vector2(0.5f, 1f);
				widget.RectTransform.localRotation = Quaternion.identity;
				// Adjust our grid settings
				widget.LayoutGroup.cellSize = new Vector2(171, 50);
				widget.LayoutGroup.spacing = Vector2.one * 4;
				widget.LayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
				widget.LayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
				widget.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
				widget.LayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				widget.LayoutGroup.constraintCount = 3;
				#endregion
				
				#region Row One
				/*Cycle mindex up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle List Up";
					button.AddButtonListener(UpdateMusicList);
					button.ButtonText.transform.localRotation = Quaternion.identity;
					_cycleControls[0] = button;
				});
				/*First Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 0;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				#endregion
				#region Row Two
				/*current mindex*/		widget.AddChild((TextWidget text) => {
					text.Text.text = "Selected:\n" + GetCurrentBankName;
					_bankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 0;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Third Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 2;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				#endregion
				#region Row Three
				/*Cycle mindex down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle List Down";
					button.AddButtonListener(UpdateMusicList);
					button.ButtonText.transform.localRotation = Quaternion.identity;
					_cycleControls[1] = button;
				});
				/*Fifth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 4;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				#endregion
				#region Row Four
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
				});
				/*Seventh Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 6;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				#endregion
				#region Row Five
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
				});
				/*Switch State*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Goto Announcer/BGM";
					button.AddButtonListener(SwitchState);
					button.ButtonText.transform.localRotation = Quaternion.identity;
					_switchstate = button;
				});
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
				});
				#endregion
				#region Row Six
				/*Cycle volume down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Decrease volume 5%";
					button.AddButtonListener(UpdateVolume);
					button.ButtonText.transform.localRotation = Quaternion.identity;
					_volControls[0] = button;
				});
				/*vol percent*/			widget.AddChild((TextWidget text) => {
					text.Text.text = GetVolumePercent();
					_volumeText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
				});
				/*Cycle volume up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Increase volume 5%";
					button.AddButtonListener(UpdateVolume);
					button.ButtonText.transform.localRotation = Quaternion.identity;
					_volControls[1] = button;
				});
				#endregion
			});
		}

		public void SwitchState(object sender, ButtonClickEventArgs args)
		{
				 if (TNHPstate == TNHPstates.BGM) TNHPstate = TNHPstates.Announcer;
			else if (TNHPstate == TNHPstates.Announcer) TNHPstate = TNHPstates.BGM;
			UpdateMusicList(null, null); //always use null as an arg, kids
			UpdateVolume(null, null);
			_bankText.Text.text = "Selected:\n" + GetCurrentBankName;
			int index = 0;
			if (TNHPstate == TNHPstates.BGM) {
				index = BankAPI.LoadedBankIndex;
			}
			if (TNHPstate == TNHPstates.Announcer) {
				index = AnnouncerAPI.LoadedAnnouncerIndex;
			}
			SetIcon(index);
		}
		
		//Updates and changes the BGMs shown
		private void UpdateMusicList(object sender, ButtonClickEventArgs args)
		{
			int cycleInc = 0;
			if (sender != null)
			{
				if (sender as ButtonWidget == _cycleControls[0])
					cycleInc = -1;
				else if (sender as ButtonWidget == _cycleControls[1])
					cycleInc = 1;
			}
			int mult = 4;
			cycleInc = mult * cycleInc;
			int NewFirstMusicIndex = _firstMusicIndex + cycleInc;
			bool oob = NewFirstMusicIndex < 0;
			if ((NewFirstMusicIndex >= BankAPI.BankLocations.Count) && TNHPstate == TNHPstates.BGM ||
			    (NewFirstMusicIndex >= AnnouncerAPI.Announcers.Count) && TNHPstate == TNHPstates.Announcer)
				oob = true;
			if (oob) {
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err); //play error audio if ran out of music to play
				return;
			}
			_firstMusicIndex = NewFirstMusicIndex;
			//update music buttons
			for (int i = 0; i < _musicButtons.Length; i++) _musicButtons[i].ButtonText.text = GetNameWithOffset(i);
		}
		
		//Updates and changes the volume amount
		private void UpdateVolume(object sender, ButtonClickEventArgs args)
		{
			float inc = 0f;
			if (sender != null)
			{
				if (sender as ButtonWidget == _volControls[0])
					inc = -0.05f;
				else if (sender as ButtonWidget == _volControls[1])
					inc = 0.05f;
			}
			
			//hahaha spaghet
			//this just updates and sets the volume n all that pizzaz. TODO: rewrite that.
			if (TNHPstate == TNHPstates.BGM) {
				PluginMain.BackgroundMusicVolume.Value += inc;
				if (PluginMain.BackgroundMusicVolume.Value < 0 || PluginMain.BackgroundMusicVolume.Value > 4)
					WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				PluginMain.BackgroundMusicVolume.Value = Mathf.Clamp(PluginMain.BackgroundMusicVolume.Value, 0, 4);
			} else if (TNHPstate == TNHPstates.Announcer) {
				PluginMain.AnnouncerMusicVolume.Value += inc;
				if (PluginMain.BackgroundMusicVolume.Value < 0 || PluginMain.BackgroundMusicVolume.Value > 20)
					WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				PluginMain.AnnouncerMusicVolume.Value = Mathf.Clamp(PluginMain.AnnouncerMusicVolume.Value, 0, 20);
			}
			_volumeText.Text.text = GetVolumePercent();
		}

		//Text Getters
		private string GetNameWithOffset(int offset)
		{
			int index = _firstMusicIndex + offset;
			if (TNHPstate == TNHPstates.BGM)
			{
				if (index < BankAPI.BankLocations.Count)
					return BankAPI.BankIndexToName(index, true);
			}
			else if (TNHPstate == TNHPstates.Announcer)
			{
				if (index < AnnouncerAPI.Announcers.Count)
				{
					//index: mybankname
					string bankname = (index + 1) + ": " + AnnouncerAPI.Announcers[index].Name;
					return bankname;
				}
			}
			return "";
		}

		private string GetCurrentBankName { get {
				if (TNHPstate == TNHPstates.BGM)
					return BankAPI.BankIndexToName(BankAPI.LoadedBankIndex, true);
				if (TNHPstate == TNHPstates.Announcer)
					return (AnnouncerAPI.LoadedAnnouncerIndex+1) + ": " + AnnouncerAPI.Announcers[AnnouncerAPI.LoadedAnnouncerIndex].Name;
				return ""; } } 
		private string GetVolumePercent()
		{
			if (TNHPstate == TNHPstates.BGM)
				return Mathf.Round(PluginMain.BackgroundMusicVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
			if (TNHPstate == TNHPstates.Announcer)
				return Mathf.Round(PluginMain.AnnouncerMusicVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
			return "";
		}

		//Sets new bank
		private void SetBank(object sender, ButtonClickEventArgs args)
		{
			var index = _firstMusicIndex + Array.IndexOf(_musicButtons, sender as ButtonWidget);
			if (GM.TNH_Manager != null) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			else
			{
				if (TNHPstate == TNHPstates.BGM) {
					BankAPI.SwapBank(index);
					GameObject go = new GameObject();
					go.AddComponent(typeof(PlaySongSnippet));
				} else if (TNHPstate == TNHPstates.Announcer) {
					var clamp = Mathf.Clamp(index, 0, AnnouncerAPI.Announcers.Count);
					AnnouncerAPI.SwapAnnouncer(AnnouncerAPI.Announcers[clamp].GUID);
					PlayAnnouncerSnippet(AnnouncerAPI.CurrentAnnouncer.GUID);
				}
				SetIcon(index);
				_bankText.Text.text = "Selected:\n" + GetCurrentBankName; //set new bank
			}
		}
		public void PlayAnnouncerSnippet(string guid)
		{
			//get first entry
			AudioClip snip = AnnouncerAPI.GetRandomPreview(AnnouncerAPI.CurrentAnnouncer.GUID);
			//make audioevent
			AudioEvent audioEvent = new AudioEvent();
			audioEvent.Clips.Add(snip);
			audioEvent.PitchRange = new Vector2(1f, 1f);
			float vol = 0.6f * PluginMain.AnnouncerMusicVolume.Value;
			audioEvent.VolumeRange = new Vector2(vol, vol);
			//play it
			SM.PlayGenericSound(audioEvent, GM.CurrentPlayerBody.transform.position);
		}
		
		private void SetIcon(int index)
		{
			if (TNHPstate == TNHPstates.BGM) {
				if (icondisplay != null) icondisplay.texture = BankAPI.LoadIconForBank(BankAPI.LoadedBankLocation);
			} else if (TNHPstate == TNHPstates.Announcer) {
				if (icondisplay != null) icondisplay.texture = AnnouncerAPI.GetAnnouncerTexture(AnnouncerAPI.Announcers[index]);
			}
		}
	}
}