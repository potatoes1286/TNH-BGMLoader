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

namespace TNHBGLoader
{
	public class TNHBackgroundMusicLoaderPanel : MonoBehaviour
	{
		public LockablePanel Panel;
		
		public TNHBackgroundMusicLoaderPanel()
		{
			//WristMenuAPI.Buttons.Add(new WristMenuButton("TNH BGM Selector", () => { SpawnPTNHBGMLPanel(); }));

			Panel = new LockablePanel();
			Panel.Configure += ConfigurePanel;
		}
		
		private TextWidget   _bankText;
		private TextWidget   _volumeText;

		private ButtonWidget[] _musicButtons = new ButtonWidget[8];
		
		/*private ButtonWidget FirstMusicButton;
		private ButtonWidget SecondMusicButton;
		private ButtonWidget ThirdMusicButton;
		private ButtonWidget FourthMusicButton;
		private ButtonWidget FifthMusicButton;
		private ButtonWidget SixthMusicButton;*/
		
		private int _firstMusicIndex;
		
		/*private void SpawnPTNHBGMLPanel()
		{
			FVRWristMenu wristMenu = WristMenuAPI.Instance;
			if (wristMenu is null || !wristMenu) return;
			GameObject panel = _PTNHBGMLpanel.GetOrCreatePanel();
			FVRPhysicalObject panelObj = panel.GetComponent<FVRPhysicalObject>();
			wristMenu.m_currentHand.RetrieveObject(panelObj);
			panelObj.SetIsKinematicLocked(true);
		}*/

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
					button.AddButtonListener(CycleUp);
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*First Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 0;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(1));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(2));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW TWO
				
				/*current mindex*/		widget.AddChild((TextWidget text) => {
					text.Text.text = "Selected:\n" + CurrentBankName;
					_bankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Third Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 2;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(3));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(4));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW THREE

				/*Cycle mindex down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle List Down";
					button.AddButtonListener(CycleDown);
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Fifth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 4;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(5));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(6));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW FOUR
				
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Seventh Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 6;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(7));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBank(8));
					_musicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW FIVE
				
				/*None*/				widget.AddChild((TextWidget text) =>
				{
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*None*/				widget.AddChild((TextWidget text) => {
					text.Text.text = "";
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW SIX
				
				/*Cycle volume down*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Decrease volume 5%";
					button.AddButtonListener(TurnDownVolume);
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*vol percent*/			widget.AddChild((TextWidget text) => {
					text.Text.text = GetVolumePercent();
					_volumeText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Cycle volume up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Increase volume 5%";
					button.AddButtonListener(TurnUpVolume);
					button.RectTransform.localRotation = Quaternion.identity;
				});
			});
		}
		
		
		//MIndex Updaters
		private void UpdateMIndex(int cycleInc)
		{
			int NewMIndex = _firstMusicIndex + cycleInc;
			if (NewMIndex < 0)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}
			if (NewMIndex >= TNHBackgroundMusicLoader.BankList.Count)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}
			_firstMusicIndex = NewMIndex;
			for (int i = 0; i < _musicButtons.Length; i++)
				_musicButtons[i].ButtonText.text = GetBankNameWithOffset(i);
		}
		private void CycleUp() => UpdateMIndex(-4); 
		private void CycleDown() => UpdateMIndex(4); 
		
		//Volume Updaters
		private void TurnUpVolume() => UpdateVolume(0.05f);
		private void TurnDownVolume() => UpdateVolume(-0.05f);
		private void UpdateVolume(float inc)
		{
			TNHBackgroundMusicLoader.BackgroundMusicVolume.Value += inc;
			if (TNHBackgroundMusicLoader.BackgroundMusicVolume.Value < 0)
			{
				TNHBackgroundMusicLoader.BackgroundMusicVolume.Value = 0;
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			} else if (TNHBackgroundMusicLoader.BackgroundMusicVolume.Value > 4)
			{
				TNHBackgroundMusicLoader.BackgroundMusicVolume.Value = 4;
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			}
			SetVolume();
		}

		//Text Getters
		private string GetBankNameWithOffset(int offset)
		{
			if (_firstMusicIndex + offset < TNHBackgroundMusicLoader.BankList.Count)
			{
				string bankname = GetBankName(_firstMusicIndex + offset, true);
				return bankname;
			}
			return "";
		}

		private string CurrentBankName => GetBankName(TNHBackgroundMusicLoader.BankIndex, true);


		private string GetBankName(int index, bool returnIndex = false)
		{
			string bankpath = TNHBackgroundMusicLoader.BankList[index];
			string bankname = Path.GetFileNameWithoutExtension(bankpath).Split('_').Last();
			if (bankname == "TAH")
				bankname = "Default";
			
			if (returnIndex) bankname = (index + 1) + ": " + bankname;
			return bankname;
		}

		private string GetMIndex()
		{
			return _firstMusicIndex.ToString();
		}
		private string GetVolumePercent()
		{
			return Mathf.Round(TNHBackgroundMusicLoader.BackgroundMusicVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
		}

		//Text Setters
		private void SetVolume() { _volumeText.Text.text = GetVolumePercent(); }

		private void SetCurrentBank() { _bankText.Text.text = "Selected:\n" + CurrentBankName;}

		private Action SetBank(int index) => () =>
			{
				if (GM.TNH_Manager != null)
				{
					WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
					return;
				}

				TNHBackgroundMusicLoader.SwapBank(index);
				SetCurrentBank();
			};
		
		
		//Bank Setting
		// private void SetBankFirst()  { SetBank(FirstMusicIndex); }
		// private void SetBankSecond() { SetBank(FirstMusicIndex + 1); }
		// private void SetBankThird()  { SetBank(FirstMusicIndex + 2); }
		// private void SetBankFourth() { SetBank(FirstMusicIndex + 3); }
		// private void SetBankFifth() { SetBank(FirstMusicIndex + 4); }
		// private void SetBankSixth() { SetBank(FirstMusicIndex + 5); }
		// private void SetBankSeventh() { SetBank(FirstMusicIndex + 6); }
		// private void SetBankEighth() { SetBank(FirstMusicIndex + 7); }
		// private void SetBank(int newBank)
		// {
		// 	if (GM.TNH_Manager != null) {
		// 		WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
		// 		return;
		// 	}
		// 	
		// 	TNH_BGM_L.SwapBank(newBank);
		// 	SetCurrentBank();
		// }
	}
}