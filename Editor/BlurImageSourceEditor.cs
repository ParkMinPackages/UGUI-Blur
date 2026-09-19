using ParkMinPackages.UGUI.Blur.Components;
using ParkMinPackages.UGUI.Blur.RendererFeatures;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ParkMinPackages.UGUI.Blur.Editor
{
	[CustomEditor(typeof(BlurImageSource))]
	internal sealed class BlurImageSourceEditor : OdinEditor
	{
		// - Handler -
		protected override void OnEnable() {
			base.OnEnable();
			_applyImageColorProperty = serializedObject.FindProperty("_applyImageColor");
			_applyImageColorContinuously = false;
			EditorApplication.update += ApplyImageColor;
		}
		protected override void OnDisable() {
			EditorApplication.update -= ApplyImageColor;
			_applyImageColorContinuously = false;
			base.OnDisable();
		}
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			serializedObject.Update();
			SirenixEditorGUI.Title("Utility", null, TextAlignment.Left, true);
			using (new EditorGUI.DisabledScope(Application.isPlaying)) {
				EditorGUILayout.BeginHorizontal();
				_applyImageColorContinuously = EditorGUILayout.ToggleLeft("일괄적용", _applyImageColorContinuously, GUILayout.Width(EditorGUIUtility.labelWidth));
				EditorGUILayout.PropertyField(_applyImageColorProperty, GUIContent.none);
				EditorGUILayout.EndHorizontal();

				if (GUILayout.Button("하위 BlurImage에 자신 할당")) {
					AssignToChildBlurImages();
				}
			}
			serializedObject.ApplyModifiedProperties();
		}

		// - Private & Protected -
		SerializedProperty _applyImageColorProperty;
		bool _applyImageColorContinuously;

		void AssignToChildBlurImages() {
			BlurImageSource source = (BlurImageSource)target;
			BlurImage[] childBlurImages = source.GetComponentsInChildren<BlurImage>(true)
				.Where(blurImage => blurImage.transform != source.transform && blurImage.Source != source)
				.ToArray();

			foreach (BlurImage blurImage in childBlurImages) {
				Undo.RecordObject(blurImage, "하위 BlurImage에 Source 할당");
				blurImage.Source = source;
				PrefabUtility.RecordPrefabInstancePropertyModifications(blurImage);
				EditorUtility.SetDirty(blurImage);
				if (blurImage.gameObject.scene.IsValid()) {
					EditorSceneManager.MarkSceneDirty(blurImage.gameObject.scene);
				}
			}
		}
		void ApplyImageColor() {
			if (_applyImageColorContinuously == false || target == null || Application.isPlaying) {
				return;
			}

			serializedObject.Update();
			Color applyImageColor = _applyImageColorProperty.colorValue;
			BlurImageSource source = (BlurImageSource)target;
			UnityEngine.UI.Image[] targetImages = Object.FindObjectsByType<BlurImage>(FindObjectsInactive.Include)
				.Where(blurImage => blurImage.Source == source)
				.Select(blurImage => blurImage.GetComponent<UnityEngine.UI.Image>())
				.Where(image => image != null && image.color != applyImageColor)
				.ToArray();

			foreach (UnityEngine.UI.Image targetImage in targetImages) {
				targetImage.color = applyImageColor;
				PrefabUtility.RecordPrefabInstancePropertyModifications(targetImage);
				EditorUtility.SetDirty(targetImage);
				if (targetImage.gameObject.scene.IsValid()) {
					EditorSceneManager.MarkSceneDirty(targetImage.gameObject.scene);
				}
			}
		}
	}
}
