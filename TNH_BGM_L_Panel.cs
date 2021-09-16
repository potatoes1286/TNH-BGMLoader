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

namespace TNH_BGLoader
{
	public class TNH_BGM_L_Panel : MonoBehaviour
	{
		public LockablePanel _PTNHBGMLpanel;
		
		public TNH_BGM_L_Panel()
		{
			//WristMenuAPI.Buttons.Add(new WristMenuButton("TNH BGM Selector", () => { SpawnPTNHBGMLPanel(); }));

			_PTNHBGMLpanel = new LockablePanel();
			_PTNHBGMLpanel.Configure += ConfigurePTNHBGMLpanel;
		}
		
		private TextWidget   BankText;
		private TextWidget   VolumeText;

		private ButtonWidget[] MusicButtons = new ButtonWidget[8];
		
		/*private ButtonWidget FirstMusicButton;
		private ButtonWidget SecondMusicButton;
		private ButtonWidget ThirdMusicButton;
		private ButtonWidget FourthMusicButton;
		private ButtonWidget FifthMusicButton;
		private ButtonWidget SixthMusicButton;*/
		
		private int FirstMusicIndex;
		
		/*private void SpawnPTNHBGMLPanel()
		{
			FVRWristMenu wristMenu = WristMenuAPI.Instance;
			if (wristMenu is null || !wristMenu) return;
			GameObject panel = _PTNHBGMLpanel.GetOrCreatePanel();
			FVRPhysicalObject panelObj = panel.GetComponent<FVRPhysicalObject>();
			wristMenu.m_currentHand.RetrieveObject(panelObj);
			panelObj.SetIsKinematicLocked(true);
		}*/

		private void ConfigurePTNHBGMLpanel(GameObject panel)
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
					button.AddButtonListener(SetBankFirst);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Second Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 1;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBankSecond);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW TWO
				
				/*current mindex*/		widget.AddChild((TextWidget text) => {
					text.Text.text = "Selected:\n" + GetCurrentBankName();
					BankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Third Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 2;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBankThird);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Fourth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 3;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBankFourth);
					MusicButtons[index] = button;
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
					button.AddButtonListener(SetBankFifth);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Sixth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 5;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBankSixth);
					MusicButtons[index] = button;
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
					button.AddButtonListener(SetBankSeventh);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Eighth Music Slot*/	widget.AddChild((ButtonWidget button) => {
					int index = 7;
					button.ButtonText.text = GetBankNameWithOffset(index);
					button.AddButtonListener(SetBankEighth);
					MusicButtons[index] = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW FIVE
				
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
					VolumeText = text;
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
			int NewMIndex = FirstMusicIndex + cycleInc;
			if (NewMIndex < 0)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}
			if (NewMIndex >= TNH_BGM_L.banks.Count)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}
			FirstMusicIndex = NewMIndex;
			for (int i = 0; i < MusicButtons.Length; i++)
				MusicButtons[i].ButtonText.text = GetBankNameWithOffset(i);
		}
		private void CycleUp() { UpdateMIndex(-4); }
		private void CycleDown() { UpdateMIndex(4); }
		
		//Volume Updaters
		private void TurnUpVolume(){UpdateVolume(0.05f);}
		private void TurnDownVolume(){UpdateVolume(-0.05f);}
		private void UpdateVolume(float inc)
		{
			TNH_BGM_L.bgmVolume.Value += inc;
			if (TNH_BGM_L.bgmVolume.Value < 0)
			{
				TNH_BGM_L.bgmVolume.Value = 0;
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			} else if (TNH_BGM_L.bgmVolume.Value > 4)
			{
				TNH_BGM_L.bgmVolume.Value = 4;
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
			}
			SetVolume();
		}

		//Text Getters
		private string GetBankNameWithOffset(int offset)
		{
			if (FirstMusicIndex + offset < TNH_BGM_L.banks.Count)
			{
				string bankname = GetBankName(FirstMusicIndex + offset, true);
				return bankname;
			}
			return "";
		}
		private string GetCurrentBankName()
		{
			return GetBankName(TNH_BGM_L.bankNum, true);
		}

		private string GetBankName(int index, bool returnIndex = false)
		{
			string bankpath = TNH_BGM_L.banks[index];
			string bankname = Path.GetFileNameWithoutExtension(bankpath).Split('_').Last();
			if (bankname == "TAH")
				bankname = "Default";
			
			if (returnIndex) bankname = (index + 1) + ": " + bankname;
			return bankname;
		}

		private string GetMIndex()
		{
			return FirstMusicIndex.ToString();
		}
		private string GetVolumePercent()
		{
			return Mathf.Round(TNH_BGM_L.bgmVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
		}

		//Text Setters
		private void SetVolume() { VolumeText.Text.text = GetVolumePercent(); }

		private void SetCurrentBank() { BankText.Text.text = "Selected:\n" + GetCurrentBankName();}

		//Bank Setting
		private void SetBankFirst()  { SetBank(FirstMusicIndex); }
		private void SetBankSecond() { SetBank(FirstMusicIndex + 1); }
		private void SetBankThird()  { SetBank(FirstMusicIndex + 2); }
		private void SetBankFourth() { SetBank(FirstMusicIndex + 3); }
		private void SetBankFifth() { SetBank(FirstMusicIndex + 4); }
		private void SetBankSixth() { SetBank(FirstMusicIndex + 5); }
		private void SetBankSeventh() { SetBank(FirstMusicIndex + 6); }
		private void SetBankEighth() { SetBank(FirstMusicIndex + 7); }
		private void SetBank(int newBank)
		{
			if (GM.TNH_Manager != null) {
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}
			
			TNH_BGM_L.SwapBank(newBank);
			SetCurrentBank();
		}
	}
}