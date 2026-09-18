using ParkMinPackages.UGUI.Blur.RendererFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ParkMinPackages.UGUI.Blur.Editor
{
	public static class UIBlurRendererFeatureInstaller
	{
		// - Public Methods -
		[MenuItem(MenuPath, priority = 100)]
		public static void AddToAllQualityLevelRenderers() {
			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				EditorUtility.DisplayDialog(DialogTitle, "Play Mode에서는 Renderer Asset을 수정할 수 없습니다.", "확인");
				return;
			}

			if (!TryGetAllQualityLevelRenderers(out List<UniversalRenderPipelineAsset> pipelineAssetList, out List<ScriptableRendererData> rendererDataList, out List<string> skippedQualityLevels, out string errorMessage)) {
				EditorUtility.DisplayDialog(DialogTitle, errorMessage, "확인");
				return;
			}

			Shader blurShader = AssetDatabase.LoadAssetAtPath<Shader>(BlurShaderPath);
			if (blurShader == null) {
				EditorUtility.DisplayDialog(DialogTitle, "패키지의 Blur Shader를 찾을 수 없습니다.\n" + BlurShaderPath, "확인");
				return;
			}

			int undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Add UI Blur Renderer Features");
			int addedCount = 0;
			int existingCount = 0;
			List<string> failures = new List<string>();
			foreach (ScriptableRendererData rendererData in rendererDataList) {
				try {
					InstallStatus status = AddRendererFeature(rendererData, blurShader, out string failureMessage);
					if (status == InstallStatus.Added) {
						addedCount++;
					}
					else if (status == InstallStatus.AlreadyInstalled) {
						existingCount++;
					}
					else {
						failures.Add(rendererData.name + ": " + failureMessage);
					}
				}
				catch (Exception exception) {
					failures.Add(rendererData.name + ": " + exception.Message);
				}
			}

			AssetDatabase.SaveAssets();
			foreach (string rendererAssetPath in rendererDataList.Select(AssetDatabase.GetAssetPath).Where(path => !string.IsNullOrEmpty(path)).Distinct()) {
				AssetDatabase.ImportAsset(rendererAssetPath);
			}
			Undo.CollapseUndoOperations(undoGroup);

			Selection.activeObject = pipelineAssetList[0];
			EditorGUIUtility.PingObject(pipelineAssetList[0]);
			string resultMessage = "전체 Quality Level: " + QualitySettings.names.Length + "개\n대상 URP Asset: " + pipelineAssetList.Count + "개\n대상 Renderer Asset: " + rendererDataList.Count + "개\n추가 완료: " + addedCount + "개\n이미 설치됨: " + existingCount + "개";
			if (skippedQualityLevels.Count > 0) {
				resultMessage += "\nURP가 아니어서 제외된 Quality Level: " + string.Join(", ", skippedQualityLevels);
			}
			if (failures.Count > 0) {
				resultMessage += "\n실패: " + failures.Count + "개\n\n" + string.Join("\n", failures);
			}
			EditorUtility.DisplayDialog(DialogTitle, resultMessage, "확인");
		}

		// - Private & Protected -
		static InstallStatus AddRendererFeature(ScriptableRendererData rendererData, Shader blurShader, out string failureMessage) {
			if (rendererData.rendererFeatures.Any(rendererFeature => rendererFeature is UIBlurRendererFeature)) {
				failureMessage = string.Empty;
				return InstallStatus.AlreadyInstalled;
			}

			string rendererAssetPath = AssetDatabase.GetAssetPath(rendererData);
			if (string.IsNullOrEmpty(rendererAssetPath)) {
				failureMessage = "저장된 Renderer Data 에셋이 아닙니다.";
				return InstallStatus.Failed;
			}

			SerializedObject serializedRendererData = new SerializedObject(rendererData);
			serializedRendererData.Update();
			SerializedProperty rendererFeatures = serializedRendererData.FindProperty("m_RendererFeatures");
			SerializedProperty rendererFeatureMap = serializedRendererData.FindProperty("m_RendererFeatureMap");
			if (rendererFeatures == null || rendererFeatureMap == null) {
				failureMessage = "Renderer Feature 목록을 찾을 수 없습니다.";
				return InstallStatus.Failed;
			}

			UIBlurRendererFeature rendererFeature = ScriptableObject.CreateInstance<UIBlurRendererFeature>();
			rendererFeature.name = nameof(UIBlurRendererFeature);
			rendererFeature.hideFlags |= HideFlags.HideInHierarchy;
			SerializedObject serializedRendererFeature = new SerializedObject(rendererFeature);
			SerializedProperty blurShaderProperty = serializedRendererFeature.FindProperty("_blurShader");
			if (blurShaderProperty == null) {
				UnityEngine.Object.DestroyImmediate(rendererFeature);
				failureMessage = "Blur Shader 필드를 찾을 수 없습니다.";
				return InstallStatus.Failed;
			}
			blurShaderProperty.objectReferenceValue = blurShader;
			serializedRendererFeature.ApplyModifiedPropertiesWithoutUndo();

			Undo.RegisterCreatedObjectUndo(rendererFeature, "Add UI Blur Renderer Feature");
			Undo.RecordObject(rendererData, "Add UI Blur Renderer Feature");
			AssetDatabase.AddObjectToAsset(rendererFeature, rendererData);
			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererFeature, out string _, out long featureLocalId)) {
				AssetDatabase.RemoveObjectFromAsset(rendererFeature);
				UnityEngine.Object.DestroyImmediate(rendererFeature);
				failureMessage = "생성한 Renderer Feature의 에셋 식별자를 가져올 수 없습니다.";
				return InstallStatus.Failed;
			}

			rendererFeatures.arraySize++;
			rendererFeatures.GetArrayElementAtIndex(rendererFeatures.arraySize - 1).objectReferenceValue = rendererFeature;
			rendererFeatureMap.arraySize++;
			rendererFeatureMap.GetArrayElementAtIndex(rendererFeatureMap.arraySize - 1).longValue = featureLocalId;
			serializedRendererData.ApplyModifiedProperties();
			rendererFeature.Create();
			EditorUtility.SetDirty(rendererFeature);
			EditorUtility.SetDirty(rendererData);
			failureMessage = string.Empty;
			return InstallStatus.Added;
		}
		static bool TryGetAllQualityLevelRenderers(out List<UniversalRenderPipelineAsset> pipelineAssetList, out List<ScriptableRendererData> rendererDataList, out List<string> skippedQualityLevels, out string errorMessage) {
			pipelineAssetList = new List<UniversalRenderPipelineAsset>();
			rendererDataList = new List<ScriptableRendererData>();
			skippedQualityLevels = new List<string>();
			string[] qualityLevelNames = QualitySettings.names;
			for (int qualityLevelIndex = 0; qualityLevelIndex < qualityLevelNames.Length; qualityLevelIndex++) {
				RenderPipelineAsset qualityPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(qualityLevelIndex);
				RenderPipelineAsset resolvedPipelineAsset = qualityPipelineAsset == null ? GraphicsSettings.defaultRenderPipeline : qualityPipelineAsset;
				if (resolvedPipelineAsset is UniversalRenderPipelineAsset pipelineAsset) {
					if (!pipelineAssetList.Contains(pipelineAsset)) {
						pipelineAssetList.Add(pipelineAsset);
					}
				}
				else {
					skippedQualityLevels.Add(qualityLevelNames[qualityLevelIndex]);
				}
			}

			if (pipelineAssetList.Count == 0) {
				errorMessage = "Quality 설정에 연결된 URP Asset이 없습니다.";
				return false;
			}

			foreach (UniversalRenderPipelineAsset pipelineAsset in pipelineAssetList) {
				SerializedObject serializedPipelineAsset = new SerializedObject(pipelineAsset);
				serializedPipelineAsset.Update();
				SerializedProperty serializedRendererDataList = serializedPipelineAsset.FindProperty("m_RendererDataList");
				if (serializedRendererDataList == null) {
					continue;
				}

				for (int rendererIndex = 0; rendererIndex < serializedRendererDataList.arraySize; rendererIndex++) {
					ScriptableRendererData rendererData = serializedRendererDataList.GetArrayElementAtIndex(rendererIndex).objectReferenceValue as ScriptableRendererData;
					if (rendererData != null && !rendererDataList.Contains(rendererData)) {
						rendererDataList.Add(rendererData);
					}
				}
			}
			if (rendererDataList.Count == 0) {
				errorMessage = "Quality 설정의 URP Asset들에 유효한 Renderer Data가 없습니다.";
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}

		// - Class Struct Enum -
		enum InstallStatus
		{
			Added,
			AlreadyInstalled,
			Failed
		}

		// - Private Statics -
		const string MenuPath = "ParkMinPackages/UGUI Blur/모든 Quality Level의 Renderer Assets에 Blur Renderer Feature 추가";
		const string DialogTitle = "UGUI Blur Renderer Feature";
		const string BlurShaderPath = "Packages/com.parkminpackages.ugui.blur/Runtime/Shaders/UIBackgroundGaussianBlur.shader";
	}
}
