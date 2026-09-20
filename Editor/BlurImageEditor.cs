using ParkMinPackages.UGUI.Blur.Components;
using ParkMinPackages.UGUI.Blur.RendererFeatures;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParkMinPackages.UGUI.Blur.Editor
{
	[CustomEditor(typeof(BlurImage))]
	internal sealed class BlurImageEditor : OdinEditor
	{
		// - Handler -
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			SirenixEditorGUI.Title("Utility", null, TextAlignment.Left, true);
			using (new EditorGUI.DisabledScope(Application.isPlaying)) {
				if (GUILayout.Button("부모 BlurImageSource 찾아서 할당")) {
					AssignParentSource();
				}
				if (GUILayout.Button("부모 BlurImageSource 색상 적용")) {
					ApplyParentSourceColor();
				}
			}
		}

		// - Private & Protected -
		void AssignParentSource() {
			BlurImage blurImage = (BlurImage)target;
			BlurImageSource source = FindParentSource(blurImage);
			if (source == null) {
				Debug.LogWarning("부모 계층에서 BlurImageSource를 찾을 수 없습니다.", blurImage);
				return;
			}
			if (blurImage.Source == source) {
				return;
			}

			Undo.RecordObject(blurImage, "부모 BlurImageSource 할당");
			blurImage.Source = source;
			RecordModification(blurImage);
		}
		void ApplyParentSourceColor() {
			BlurImage blurImage = (BlurImage)target;
			BlurImageSource source = FindParentSource(blurImage);
			if (source == null) {
				Debug.LogWarning("부모 계층에서 BlurImageSource를 찾을 수 없습니다.", blurImage);
				return;
			}

			SerializedObject serializedSource = new SerializedObject(source);
			SerializedProperty colorProperty = serializedSource.FindProperty("_applyImageColor");
			if (colorProperty == null) {
				Debug.LogWarning("BlurImageSource의 Utility 색상 필드를 찾을 수 없습니다.", source);
				return;
			}

			UnityEngine.UI.Image image = blurImage.GetComponent<UnityEngine.UI.Image>();
			if (image.color == colorProperty.colorValue) {
				return;
			}

			Undo.RecordObject(image, "부모 BlurImageSource 색상 적용");
			image.color = colorProperty.colorValue;
			RecordModification(image);
		}
		BlurImageSource FindParentSource(BlurImage blurImage) {
			Transform parent = blurImage.transform.parent;
			return parent == null ? null : parent.GetComponentInParent<BlurImageSource>(true);
		}
		void RecordModification(Object modifiedObject) {
			PrefabUtility.RecordPrefabInstancePropertyModifications(modifiedObject);
			EditorUtility.SetDirty(modifiedObject);
			if (modifiedObject is Component component && component.gameObject.scene.IsValid()) {
				EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
			}
		}
	}
}
