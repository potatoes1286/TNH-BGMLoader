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
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetBankNameWithOffset(index);
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
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetBankNameWithOffset(index);
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
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetBankNameWithOffset(index);
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
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
					button.ButtonText.transform.localRotation = Quaternion.identity;
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetBankNameWithOffset(index);
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
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
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
		
		#region BankHandler
		//Updates and changes the BGMs shown
		private void UpdateMusicList(object sender, ButtonClickEventArgs args)
		{
			int cycleInc = 0;
			if		(sender as ButtonWidget == _cycleControls[0]) { cycleInc = -1; }
			else if (sender as ButtonWidget == _cycleControls[1]) { cycleInc = 1; }
			int mult = 4;
			cycleInc = mult * cycleInc;
			int NewFirstMusicIndex = _firstMusicIndex + cycleInc;
			
			if (NewFirstMusicIndex < 0 || NewFirstMusicIndex >= BankAPI.BankList.Count)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err); //play error audio if ran out of music to play
				return;
			}
			_firstMusicIndex = NewFirstMusicIndex;
			//update music buttons
			for (int i = 0; i < _musicButtons.Length; i++) _musicButtons[i].ButtonText.text = GetBankNameWithOffset(i);
		}
		
		//Updates and changes the volume amount
		private void UpdateVolume(object sender, ButtonClickEventArgs args)
		{
			float inc = 0f;
			if		(sender as ButtonWidget == _volControls[0]) { inc = -0.05f; } 
			else if (sender as ButtonWidget == _volControls[1]) { inc = 0.05f; }
			PluginMain.BackgroundMusicVolume.Value += inc;
			if (PluginMain.BackgroundMusicVolume.Value < 0 || PluginMain.BackgroundMusicVolume.Value > 4)
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			PluginMain.BackgroundMusicVolume.Value = Mathf.Clamp(PluginMain.BackgroundMusicVolume.Value, 0, 4);
			_volumeText.Text.text = GetVolumePercent();  //set volume
		}

		//Text Getters
		private string GetBankNameWithOffset(int offset)
		{
			if (_firstMusicIndex + offset < BankAPI.BankList.Count)
			{
				string bankname = GetBankName(_firstMusicIndex + offset, true);
				return bankname;
			}
			return "";
		}
		private string GetCurrentBankName => GetBankName(BankAPI.BankIndex, true);
		public static string GetBankName(int index, bool returnIndex = false)
		{
			string bankpath = BankAPI.BankList[index];
			string bankname = Path.GetFileNameWithoutExtension(bankpath).Split('_').Last();
			if (bankname == "TAH")
				bankname = "Default";
			
			if (returnIndex) bankname = (index + 1) + ": " + bankname;
			return bankname;
		}
		private string GetVolumePercent() => Mathf.Round(PluginMain.BackgroundMusicVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
		
		//Sets new bank
		private void SetBank(object sender, ButtonClickEventArgs args)
		{
			var index = _firstMusicIndex + Array.IndexOf(_musicButtons, sender as ButtonWidget);
			if (GM.TNH_Manager != null) WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			else
			{
				BankAPI.SwapBank(index);
				_bankText.Text.text = "Selected:\n" + GetCurrentBankName; //set new bank
				if (icondisplay != null)
				{
					icondisplay.texture = BankAPI.LoadIconForBank(BankAPI.loadedBank);
				}
				else Debug.Log("What's a icon displayer?");
				GameObject go = new GameObject();
				go.AddComponent(typeof(PlaySongSnippet));
			}
		}
		#endregion
	}
}