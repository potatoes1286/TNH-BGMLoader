using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FistVR;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using Sodalite;
using Sodalite.Api;
using Sodalite.UiWidgets;
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
		}
		
		private TextWidget   _bankText;
		private TextWidget   _volumeText;

		private ButtonWidget[] _musicButtons = new ButtonWidget[8];

		private int _firstMusicIndex;

		private void ConfigurePanel(GameObject panel)
		{
			GameObject canvas = panel.transform.Find("OptionsCanvas_0_Main/Canvas").gameObject;
			UiWidget.CreateAndConfigureWidget(canvas, (GridLayoutWidget widget) =>
			{
				//init panel; this if is to just let rider fuckign collapse this shit man
				if(true){
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
				}
				
				//ROW ONE
				
				/*Cycle mindex up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle List Up";
					button.AddButtonListener((_,__) => UpdateMusicList(-1));
				});
				/*First Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 0;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				
				//ROW TWO
				
				/*current mindex*/		widget.AddChild((TextWidget text) => {
					text.Text.text = "Selected:\n" + GetCurrentBankName;
					_bankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Third Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 2;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				
				//ROW THREE

				/*Cycle mindex down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle List Down";
					button.AddButtonListener((_, __) => UpdateMusicList(1));
				});
				/*Fifth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 4;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				
				//ROW FOUR
				
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
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank);
					_musicButtons[index] = button;
				});
				
				//ROW FIVE
				
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
				
				//ROW SIX
				
				/*Cycle volume down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Decrease volume 5%";
					button.AddButtonListener((_, __) => UpdateVolume(-0.05f));
				});
				/*vol percent*/			widget.AddChild((TextWidget text) => {
					text.Text.text = GetVolumePercent();
					_volumeText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
				});
				/*Cycle volume up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Increase volume 5%";
					button.AddButtonListener((_, __) => UpdateVolume(0.05f));
				});
			});
		}
		
		//Updates and changes the BGMs shown
		private Action<object, ButtonClickEventArgs> UpdateMusicList(int cycleInc) => (_, __) =>
		{
			int mult = 4;
			cycleInc = mult * cycleInc;
			int NewFirstMusicIndex = _firstMusicIndex + cycleInc;
			
			if (NewFirstMusicIndex < 0 || NewFirstMusicIndex >= PluginMain.BankList.Count)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err); //play error audio if ran out of music to play
				return;
			}
			_firstMusicIndex = NewFirstMusicIndex;
			//update music buttons
			for (int i = 0; i < _musicButtons.Length; i++) _musicButtons[i].ButtonText.text = GetBankNameWithOffset(i);
		};
		
		//Updates and changes the volume amount
		private Action<object, ButtonClickEventArgs> UpdateVolume(float inc) => (_, __) =>
		{
			PluginMain.BackgroundMusicVolume.Value += inc;
			if (PluginMain.BackgroundMusicVolume.Value < 0 || PluginMain.BackgroundMusicVolume.Value > 4)
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			PluginMain.BackgroundMusicVolume.Value = Mathf.Clamp(PluginMain.BackgroundMusicVolume.Value, 0, 4);
			_volumeText.Text.text = GetVolumePercent();  //set volume
		};

		//Text Getters
		private string GetBankNameWithOffset(int offset)
		{
			if (_firstMusicIndex + offset < PluginMain.BankList.Count)
			{
				string bankname = GetBankName(_firstMusicIndex + offset, true);
				return bankname;
			}
			return "";
		}
		private string GetCurrentBankName => GetBankName(PluginMain.BankIndex, true);
		private string GetBankName(int index, bool returnIndex = false)
		{
			string bankpath = PluginMain.BankList[index];
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
				PluginMain.SwapBank(index);
				_bankText.Text.text = "Selected:\n" + GetCurrentBankName; //set new bank
			}
		}
	}
}