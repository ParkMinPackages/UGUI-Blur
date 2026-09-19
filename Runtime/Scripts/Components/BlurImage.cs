using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ParkMinPackages.UGUI.Blur.Components
{
	[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Image))]
	public sealed class BlurImage : UnityEngine.UI.BaseMeshEffect, UnityEngine.UI.IMaterialModifier
	{
		// - Public Methods -
		public Material GetModifiedMaterial(Material baseMaterial) {
			if (IsActive() && _materialTemplate != null) {
				if (_material == null || _baseMaterial != baseMaterial) {
					CoreUtils.Destroy(_material);
					_baseMaterial = baseMaterial;
					_material = new Material(baseMaterial) { shader = _materialTemplate.shader, hideFlags = HideFlags.HideAndDontSave, name = "UI Blur (Instance)" };
				}
				UpdateMaterialProperties();
				return _material;
			}
			return baseMaterial;
		}
		public override void ModifyMesh(UnityEngine.UI.VertexHelper vertices) {
			if (IsActive() && graphic.canvas != null) {
				Camera camera = graphic.canvas.rootCanvas.worldCamera;
				Rect screen = camera == null ? new Rect(0f, 0f, Screen.width, Screen.height) : camera.pixelRect;
				UIVertex vertex = default;
				for (int i = 0; i < vertices.currentVertCount; i++) {
					vertices.PopulateUIVertex(ref vertex, i);
					Vector2 position = RectTransformUtility.WorldToScreenPoint(camera, transform.TransformPoint(vertex.position));
					vertex.color = Color.white;
					vertex.uv1 = new Vector4((position.x - screen.x) / screen.width, (position.y - screen.y) / screen.height, 0f, 0f);
					vertices.SetUIVertex(vertex, i);
				}
			}
		}

		// - Handler -
		protected override void OnEnable() {
			base.OnEnable();
			MigrateLegacyTintColor();
			Canvas.preWillRenderCanvases += OnCanvasRendering;
			graphic.SetMaterialDirty();
		}
		protected override void OnDisable() {
			Canvas.preWillRenderCanvases -= OnCanvasRendering;
			CoreUtils.Destroy(_material);
			_material = null;
			_baseMaterial = null;
			graphic.SetMaterialDirty();
			base.OnDisable();
		}
		void OnCanvasRendering() {
			if (graphic.canvas != null) {
				graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
				UpdateMaterialProperties();
				graphic.SetVerticesDirty();
			}
		}
#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			MigrateLegacyTintColor();
			if (graphic != null) {
				graphic.SetMaterialDirty();
				graphic.SetVerticesDirty();
			}
		}
#endif

		// - Private & Protected -
		[Header(Headers.Required)]
		[SerializeField, Required] Material _materialTemplate;

		[SerializeField, HideInInspector, FormerlySerializedAs("_tintColor")] Color _legacyTintColor = new Color(-1f, -1f, -1f, -1f);
		[SerializeField, HideInInspector] bool _imageColorMigrated;

		Material _material;
		Material _baseMaterial;

		void MigrateLegacyTintColor() {
			if (_imageColorMigrated) {
				return;
			}

			_imageColorMigrated = true;
			if (_legacyTintColor.a >= 0f && graphic != null) {
				graphic.color = _legacyTintColor;
			}
		}
		void UpdateMaterialProperties() {
			if (_material != null && graphic != null) {
				_material.SetColor(_tintColorPropertyId, graphic.color);
				_material.SetFloat(_opacityPropertyId, 1f);
			}
		}

		// - Private Statics -
		static readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
		static readonly int _opacityPropertyId = Shader.PropertyToID("_Opacity");
	}
}
