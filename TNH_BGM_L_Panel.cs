﻿using System.IO;
using FistVR;
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
			WristMenuAPI.Buttons.Add(new WristMenuButton("Spawn Utils Panel", () => { SpawnPTNHBGMLPanel(); }));

			_PTNHBGMLpanel = new LockablePanel();
			_PTNHBGMLpanel.Configure += ConfigurePTNHBGMLpanel;
		}

		private ButtonWidget CycleMusicUp;
		private ButtonWidget CycleMusicDown;
		private TextWidget   bankText;
		
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

				//ROW ONE
				widget.AddChild((ButtonWidget button) =>
				{
					button.ButtonText.text = "Cycle Up";
					button.AddButtonListener(CycleUp);
					CycleMusicUp = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});

				widget.AddChild((TextWidget text) =>
				{
					text.Text.text = "banknamehere!";
					bankText = text;
					text.Text.alignment = TextAnchor.MiddleCenter;
					text.Text.fontSize += 5;
					text.RectTransform.localRotation = Quaternion.identity;
				});

				widget.AddChild((ButtonWidget button) =>
				{
					button.ButtonText.text = "Cycle Down";
					button.AddButtonListener(CycleDown);
					CycleMusicDown = button;
					button.RectTransform.localRotation = Quaternion.identity;
				});
			});
		}
		private void UpdateMusic(int cycleInc)
		{
			TNH_BGM_L.SwapBanks(TNH_BGM_L.bankNum += cycleInc);
			bankText.Text.text = Path.GetFileName(TNH_BGM_L.relevantBank);
		}
		private void CycleUp() { UpdateMusic(1); }
		private void CycleDown() { UpdateMusic(-1); }
	}
}