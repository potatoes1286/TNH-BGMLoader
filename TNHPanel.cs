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
using TNHBGLoader.Sosig;
using TNHBGLoader.Soundtrack;
using Valve.Newtonsoft.Json.Utilities;

namespace TNHBGLoader
{
	public class TNHPanel : MonoBehaviour
	{
		public LockablePanel Panel;
		public enum TNHPstates { BGM, Announcer, Sosig_Voicelines }
		public TNHPstates TNHPstate = TNHPstates.BGM;

		public TNHPanel()
		{
			Panel = new LockablePanel();
			Panel.Configure += ConfigurePanel;
			Panel.TextureOverride = SodaliteUtils.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("panel.png"));
		}
		
		private TextWidget   _bankText;
		private TextWidget   _volumeText;

		private ButtonWidget[] _musicButtons  = new ButtonWidget[8];
		private ButtonWidget[] _volControls   = new ButtonWidget[2];
		private ButtonWidget[] _cycleControls = new ButtonWidget[2];
		private ButtonWidget   _switchstate;

		private int _firstMusicIndex;

		public RawImage icondisplay;

		private int _selectedVLSSet;

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
					button.AddButtonListener(SetCurrentItem);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetCurrentItem);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				#endregion
				#region Row Two
				/*current mindex*/		widget.AddChild((TextWidget text) => {
					text.Text.text = "Selected:\n" + GetCurrentBankName();
					_bankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 0;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Third Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 2;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetCurrentItem);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetCurrentItem);
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
					button.AddButtonListener(SetCurrentItem);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetCurrentItem);
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
					button.AddButtonListener(SetCurrentItem);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetNameWithOffset(index);
					button.AddButtonListener(SetCurrentItem);
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
					button.ButtonText.text = "BGM";
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
			//beauty.
				 if (TNHPstate == TNHPstates.BGM) {TNHPstate = TNHPstates.Announcer;}
			else if (TNHPstate == TNHPstates.Announcer) {TNHPstate = TNHPstates.Sosig_Voicelines;} 
			else if (TNHPstate == TNHPstates.Sosig_Voicelines) {TNHPstate = TNHPstates.BGM;}
				 
			_switchstate.ButtonText.text = TNHPstate.ToString().Replace('_', ' ');
			_bankText.Text.text = "Selected:\n" + GetCurrentBankName();
			_firstMusicIndex = 0;
			int index = 0;
			UpdateMusicList(null, null); //always use null as an arg, kids
			UpdateVolume(null, null);
			SetIcon();
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
			switch (TNHPstate) {
				case TNHPstates.BGM:
					if (NewFirstMusicIndex >= BankAPI.LoadedBankLocations.Count + SoundtrackAPI.Soundtracks.Length) oob = true;
					break;
				case TNHPstates.Announcer:
					if (NewFirstMusicIndex >= AnnouncerAPI.LoadedAnnouncers.Count) oob = true;
					break;
				case TNHPstates.Sosig_Voicelines:
					if (NewFirstMusicIndex >= SosigVLSAPI.LoadedSosigVLSs.Count) oob = true;
					break;
			}
			if (oob) {
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err); //play error audio if ran out of music to play
				return;
			}
			_firstMusicIndex = NewFirstMusicIndex;
			//update music buttons
			for (int i = 0; i < _musicButtons.Length; i++) _musicButtons[i].ButtonText.text = GetNameWithOffset(i);
		}
		
		//When in VLS, the audio volume switches to a "VLS type" option to select the VLS for different
		//audio types, such as Zosigs or those funny little robotic men.
		private void CycleVLS(object? sender)
		{
			int[] range = new[] { 0, SosigVLSAPI.VLSGuidOrder.Count - 1 };
			//++ if volcontrols 1, -- if volcontrols 0
			//then wrap to prevent overflow/underflow
			if(sender != null)
				_selectedVLSSet = (_selectedVLSSet + (sender as ButtonWidget == _volControls[1] ? 1 : -1)).Wrap(range);
			_volumeText.Text.text = SosigVLSAPI.VLSGuidToName[SosigVLSAPI.VLSGuidOrder[_selectedVLSSet]];
			_volControls[0].ButtonText.text = SosigVLSAPI.VLSGuidToName[SosigVLSAPI.VLSGuidOrder[(_selectedVLSSet - 1).Wrap(range)]];
			_volControls[1].ButtonText.text = SosigVLSAPI.VLSGuidToName[SosigVLSAPI.VLSGuidOrder[(_selectedVLSSet + 1).Wrap(range)]];
			PlaySnippet(SosigVLSAPI.GetRandomPreview(SosigVLSAPI.CurrentSosigVlsOfVlsSet(_selectedVLSSet).guid));
		}
		
		//Updates and changes the volume amount
		private void UpdateVolume(object sender, ButtonClickEventArgs args)
		{
			if (TNHPstate == TNHPstates.Sosig_Voicelines)
			{
				CycleVLS(sender);
				return;
			}

			float inc = 0f;
			if (sender != null)
			{
				if (sender as ButtonWidget == _volControls[0])
					inc = -0.05f;
				else if (sender as ButtonWidget == _volControls[1])
					inc = 0.05f;
			}
			
			//this just updates and sets the volume n all that pizzaz.
			switch (TNHPstate)
			{
				case TNHPstates.BGM:
					PluginMain.BackgroundMusicVolume.Value += inc;
					if (!GeneralAPI.IfIsInRange(PluginMain.BackgroundMusicVolume.Value, 0, 4)) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
					PluginMain.BackgroundMusicVolume.Value = Mathf.Clamp(PluginMain.BackgroundMusicVolume.Value, 0, 4);
					break;
				case TNHPstates.Announcer:
					PluginMain.AnnouncerMusicVolume.Value += inc;
					if (!GeneralAPI.IfIsInRange(PluginMain.AnnouncerMusicVolume.Value, 0, 20)) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
					PluginMain.AnnouncerMusicVolume.Value = Mathf.Clamp(PluginMain.AnnouncerMusicVolume.Value, 0, 20);
					break;
				/*case TNHPstates.Sosig_Voicelines:
					if(inc != 0) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
					break;*/
			}
			_volumeText.Text.text = GetVolumePercent();
		}

		//Text Getters
		private string GetNameWithOffset(int offset)
		{
			int index = _firstMusicIndex + offset;
			switch (TNHPstate) {
				case TNHPstates.BGM:
					if (index < BankAPI.LoadedBankLocations.Count)
						return BankAPI.GetNameFromIndex(index, true);
					if (index < BankAPI.LoadedBankLocations.Count + SoundtrackAPI.Soundtracks.Length)
						return SoundtrackAPI.Soundtracks[index - BankAPI.LoadedBankLocations.Count].Name;
					break;
				case TNHPstates.Announcer:
					if (index < AnnouncerAPI.LoadedAnnouncers.Count)
						return (index + 1) + ": " + AnnouncerAPI.LoadedAnnouncers[index].Name;
					break;
				case TNHPstates.Sosig_Voicelines:
					if (index < SosigVLSAPI.LoadedSosigVLSs.Count)
						return (index + 1) + ": " + SosigVLSAPI.LoadedSosigVLSs[index].name;
					break;
			}
			return "";
		}

		private string GetCurrentBankName() {
			switch (TNHPstate)
			{
				case TNHPstates.BGM:
					if(!SoundtrackAPI.SoundtrackEnabled)
						return BankAPI.GetNameFromIndex(BankAPI.CurrentBankIndex, true);
					return $"{BankAPI.LoadedBankLocations.Count + SoundtrackAPI.SelectedSoundtrack + 1}: {SoundtrackAPI.Soundtracks[SoundtrackAPI.SelectedSoundtrack]}";
				case TNHPstates.Announcer:
					return (AnnouncerAPI.CurrentAnnouncerIndex+1) + ": " + AnnouncerAPI.CurrentAnnouncer.Name;
				case TNHPstates.Sosig_Voicelines:
					return (SosigVLSAPI.CurrentSosigVlsIndexOfVlsSet(_selectedVLSSet)+1) + ": " + SosigVLSAPI.CurrentSosigVlsOfVlsSet(_selectedVLSSet).name;
			} return ""; }
		private string GetVolumePercent()
		{
			float vol = 0;
			switch (TNHPstate) {
				case TNHPstates.BGM:
					vol = PluginMain.BackgroundMusicVolume.Value;
					break;
				case TNHPstates.Announcer:
					vol = PluginMain.AnnouncerMusicVolume.Value;
					break;
				case TNHPstates.Sosig_Voicelines:
					vol = 1;
					break;
			}
			return Mathf.Round(vol * 100).ToString(CultureInfo.InvariantCulture) + "%";
		}
		
		private void SetCurrentItem(object sender, ButtonClickEventArgs args)
		{
			var index = _firstMusicIndex + Array.IndexOf(_musicButtons, sender as ButtonWidget);
			if (GM.TNH_Manager != null) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			else
			{
				int clamp;
				switch (TNHPstate)
				{
					case TNHPstates.BGM:
						BankAPI.SwapBank(index);
						if (index < BankAPI.LoadedBankLocations.Count) {
							GameObject go = new GameObject();
							go.AddComponent(typeof(PlayFModSnippet));
						}
						else {
							//TODO: Play snippet for soundtrack!
						}

						break;
					case TNHPstates.Announcer:
						clamp = Mathf.Clamp(index, 0, AnnouncerAPI.LoadedAnnouncers.Count);
						AnnouncerAPI.SwapAnnouncer(AnnouncerAPI.LoadedAnnouncers[clamp].GUID);
						PlaySnippet(AnnouncerAPI.GetRandomPreview(AnnouncerAPI.CurrentAnnouncer.GUID));
						break;
					case TNHPstates.Sosig_Voicelines:
						clamp = Mathf.Clamp(index, 0, SosigVLSAPI.LoadedSosigVLSs.Count);
						SosigVLSAPI.SwapSosigVLS(SosigVLSAPI.LoadedSosigVLSs[clamp].guid, SosigVLSAPI.VLSGuidOrder[_selectedVLSSet]);
						PlaySnippet(SosigVLSAPI.GetRandomPreview(SosigVLSAPI.CurrentSosigVlsOfVlsSet(_selectedVLSSet).guid));
						break;
				}
				SetIcon();
				_bankText.Text.text = "Selected:\n" + GetCurrentBankName(); //set new bank
			}
		}
		public void PlaySnippet(AudioClip snip)
		{
			float pitch = 1f;
			float vol = 0.6f;
			switch (TNHPstate)
			{
				case TNHPstates.Announcer:
					vol = 0.6f * PluginMain.AnnouncerMusicVolume.Value;
					break;
				case TNHPstates.Sosig_Voicelines:
					pitch = SosigVLSAPI.CurrentSosigVlsOfVlsSet(_selectedVLSSet).SpeechSet.BasePitch;
					break;
			}
			//make audioevent
			AudioEvent audioEvent = new AudioEvent();
			audioEvent.Clips.Add(snip);
			audioEvent.PitchRange = new Vector2(pitch, pitch);
			audioEvent.VolumeRange = new Vector2(vol, vol);
			//play it
			SM.PlayGenericSound(audioEvent, GM.CurrentPlayerBody.transform.position);
		}
		
		private void SetIcon()
		{
			if (icondisplay == null) return;
			switch (TNHPstate)
			{
				case TNHPstates.BGM:
					if(!SoundtrackAPI.SoundtrackEnabled)
						icondisplay.texture = BankAPI.GetBankIcon(BankAPI.CurrentBankLocation);
					else
						icondisplay.texture = SoundtrackAPI.GetIcon(SoundtrackAPI.SelectedSoundtrack);
					break;
				case TNHPstates.Announcer:
					icondisplay.texture = AnnouncerAPI.GetAnnouncerIcon(AnnouncerAPI.CurrentAnnouncer);
					break;
				case TNHPstates.Sosig_Voicelines:
					icondisplay.texture = SosigVLSAPI.GetSosigVLSIcon(SosigVLSAPI.CurrentSosigVlsOfVlsSet(_selectedVLSSet));
					break;
			}
		}
	}
}