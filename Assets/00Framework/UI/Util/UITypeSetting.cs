using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LSJ.UI {
	/// <summary>
	/// 오브젝트들의 원하는 세팅들을 미리 저장 후 ID값을 입력하여 그 세팅이 나타나도록 하는 기능
	/// </summary>
	public class UITypeSetting : MonoBehaviour {
		[ReadOnly] public int currentId;
		public List<UITypeSettingInfo> infoList;

		public void Active(int id) {
			if (infoList == null) {
				return;
			}

			if (id >= infoList.Count) {
				return;
			}

			UITypeSettingInfo settingInfo = infoList[id];
			if (settingInfo == null) {
				return;
			}

			currentId = id;
			settingInfo.Active();
		}

		public void Active<TEnum>(TEnum id) where TEnum : Enum {
			try {
				Active(Convert.ToInt32(id));
			} catch (Exception e) {
				Active(0);
			}
		}

		public void Active(bool isActive) {
			Active(isActive ? 1 : 0);
		}

		[Serializable]
		public class UITypeSettingInfo {
			[Tooltip("스크립트 enable 설정")]
			public List<UITypeSettingEnable> enableList;

			[Tooltip("게임오브젝트 active 설정")]
			public List<UITypeSettingGameObject> gameobjectList;

			[Tooltip("이미지 sprite 설정")]
			public List<UITypeSettingSprite> spriteList;

			[Tooltip("색상 설정")]
			public List<UITypeSettingGraphicColor> graphicColorList;

			[Tooltip("RectTransform 값 설정")]
			public List<UITypeSettingRectTransform> rectTransformList;

			[Tooltip("텍스트 문구 설정")]
			public List<UITypeSettingText> textList;

			public void Active() {
				if (enableList != null) {
					foreach (UITypeSettingEnable uiTypeSettingEnable in enableList) {
						uiTypeSettingEnable.Active();
					}
				}

				if (gameobjectList != null) {
					foreach (UITypeSettingGameObject uiTypeSettingGameObject in gameobjectList) {
						uiTypeSettingGameObject.Active();
					}
				}

				if (spriteList != null) {
					foreach (UITypeSettingSprite uiTypeSettingSprite in spriteList) {
						uiTypeSettingSprite.Active();
					}
				}

				if (graphicColorList != null) {
					foreach (UITypeSettingGraphicColor uiTypeSettingGraphicColor in graphicColorList) {
						uiTypeSettingGraphicColor.Active();
					}
				}

				if (rectTransformList != null) {
					foreach (UITypeSettingRectTransform uiTypeSettingRectTransform in rectTransformList) {
						uiTypeSettingRectTransform.Active();
					}
				}

				if (textList != null) {
					foreach (UITypeSettingText uiTypeSettingText in textList) {
						uiTypeSettingText.Active();
					}
				}
			}

			public void Copy(UITypeSettingInfo copyInfo) {
				enableList.Clear();
				gameobjectList.Clear();
				spriteList.Clear();
				graphicColorList.Clear();
				rectTransformList.Clear();

				foreach (UITypeSettingEnable uiTypeSettingEnable in copyInfo.enableList) {
					enableList.Add(uiTypeSettingEnable);
				}

				foreach (UITypeSettingGameObject uiTypeSettingGameObject in copyInfo.gameobjectList) {
					gameobjectList.Add(uiTypeSettingGameObject);
				}

				foreach (UITypeSettingSprite uiTypeSettingSprite in copyInfo.spriteList) {
					spriteList.Add(uiTypeSettingSprite);
				}

				foreach (UITypeSettingGraphicColor uiTypeSettingGraphicColor in copyInfo.graphicColorList) {
					graphicColorList.Add(uiTypeSettingGraphicColor);
				}

				foreach (UITypeSettingRectTransform uiTypeSettingRectTransform in copyInfo.rectTransformList) {
					rectTransformList.Add(uiTypeSettingRectTransform);
				}
			}
		}

		public abstract class UITypeSettingTypes {
			public abstract void Active();
		}

		[Serializable]
		public class UITypeSettingEnable : UITypeSettingTypes {
			public Behaviour behaviour;
			public bool isEnable;

			public override void Active() {
				if (behaviour != null) {
					behaviour.enabled = isEnable;
				}
			}
		}

		[Serializable]
		public class UITypeSettingGameObject : UITypeSettingTypes {
			public GameObject gameObject;
			public bool isActive;

			public override void Active() {
				if (gameObject != null) {
					gameObject.SetActive(isActive);
				}
			}
		}

		[Serializable]
		public class UITypeSettingSprite : UITypeSettingTypes {
			public Image image;
			public Sprite sprite;
			public bool isSetActiveNative;

			public override void Active() {
				if (image != null) {
					image.sprite = sprite;
					if (isSetActiveNative) {
						image.SetNativeSize();
					}
				}
			}
		}

		[Serializable]
		public class UITypeSettingGraphicColor : UITypeSettingTypes {
			public Graphic graphic;
			public Color color;

			public override void Active() {
				if (graphic != null) {
					graphic.color = color;
				}
			}
		}

		[Serializable]
		public class UITypeSettingRectTransform : UITypeSettingTypes {
			public RectTransform rectTransform;
			public Vector3 scale;

			public override void Active() {
				if (rectTransform != null) {
					rectTransform.localScale = scale;
				}
			}
		}

		[Serializable]
		public class UITypeSettingText : UITypeSettingTypes {
			public TextMeshProUGUI text;
			public string textString;

			public override void Active() {
				text.text = textString;
			}
		}
	}
}