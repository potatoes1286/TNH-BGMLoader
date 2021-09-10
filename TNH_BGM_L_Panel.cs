using System.Globalization;
using System.IO;
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
		private LockablePanel _PTNHBGMLpanel;

		public TNH_BGM_L_Panel()
		{
			WristMenuAPI.Buttons.Add(new WristMenuButton("TNH BGM Selector", () => { SpawnPTNHBGMLPanel(); }));

			_PTNHBGMLpanel = new LockablePanel();
			_PTNHBGMLpanel.Configure += ConfigurePTNHBGMLpanel;
		}

		private ButtonWidget CycleMusicUp;
		private ButtonWidget CycleMusicDown;
		private TextWidget   BankText;

		private ButtonWidget IncreaseVolume;
		private ButtonWidget DecreaseVolume;
		private TextWidget   VolumeText;

		private void SpawnPTNHBGMLPanel()
		{
			FVRWristMenu wristMenu = WristMenuAPI.Instance;
			if (wristMenu is null || !wristMenu) return;
			GameObject panel = _PTNHBGMLpanel.GetOrCreatePanel();
			wristMenu.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}

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
				/*Cycle bank up*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle Up";
					button.AddButtonListener(CycleUp);
					IncreaseVolume = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Show current bank*/	widget.AddChild((TextWidget text) => {
					text.Text.text = Path.GetFileName(TNH_BGM_L.relevantBank);
					BankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Cycle bank down*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Cycle Down";
					button.AddButtonListener(CycleDown);
					CycleMusicDown = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
				//ROW TWO
				/*Turn up volume*/		widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Decrease volume 5%";
					button.AddButtonListener(TurnDownVolume);
					DecreaseVolume = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				/*Current volume*/		widget.AddChild((TextWidget text) => {
					text.Text.text = GetVolumePercent();
					VolumeText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});
				/*Turn down volume*/	widget.AddChild((ButtonWidget button) => {
					button.ButtonText.text = "Increase volume 5%";
					button.AddButtonListener(TurnUpVolume);
					CycleMusicUp = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
				
			});
		}
		private void UpdateMusic(int cycleInc)
		{
			if (GM.TNH_Manager != null)
			{
				WristMenuAPI.Instance.Aud.PlayOneShot(WristMenuAPI.Instance.AudClip_Err);
				return;
			}

			int newBN = TNH_BGM_L.bankNum + cycleInc;
			TNH_BGM_L.SwapBank(newBN);
			BankText.Text.text = Path.GetFileName(TNH_BGM_L.relevantBank);
		}
		private void CycleUp() { UpdateMusic(1); }
		private void CycleDown() { UpdateMusic(-1); }
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
			VolumeText.Text.text = GetVolumePercent();
		}
		private string GetVolumePercent()
		{
			return Mathf.Round(TNH_BGM_L.bgmVolume.Value * 100).ToString(CultureInfo.InvariantCulture) + "%";
		}
		private void TurnUpVolume(){UpdateVolume(0.05f);}
		private void TurnDownVolume(){UpdateVolume(-0.05f);}
	}
}