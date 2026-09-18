using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParkMinPackages.UGUI.Blur.Components
{
	[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Image))]
	public sealed class UIBackgroundBlur : UnityEngine.UI.BaseMeshEffect, UnityEngine.UI.IMaterialModifier
	{
		// - Public Methods -
		public Material GetModifiedMaterial(Material baseMaterial) {
			if (IsActive() && _materialTemplate != null) {
				if (_material == null || _baseMaterial != baseMaterial) {
					CoreUtils.Destroy(_material);
					_baseMaterial = baseMaterial;
					_material = new Material(baseMaterial) { shader = _materialTemplate.shader, hideFlags = HideFlags.HideAndDontSave, name = "UI Background Blur (Instance)" };
				}
				_material.SetColor("_TintColor", _tintColor);
				_material.SetFloat("_Opacity", _opacity);
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
					vertex.uv1 = new Vector4((position.x - screen.x) / screen.width, (position.y - screen.y) / screen.height, 0f, 0f);
					vertices.SetUIVertex(vertex, i);
				}
			}
		}

		// - Handler -
		protected override void OnEnable() {
			base.OnEnable();
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
				graphic.SetVerticesDirty();
			}
		}
#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			if (graphic != null) {
				graphic.SetMaterialDirty();
			}
		}
#endif

		// - Private & Protected -
		[Header(Headers.Required)]
		[SerializeField, Required] Material _materialTemplate;

		[Header(Headers.Settings)]
		[SerializeField] Color _tintColor = new Color(0.02f, 0.26f, 0.46f, 0.90f);
		[SerializeField, Range(0f, 1f)] float _opacity = 1f;

		Material _material;
		Material _baseMaterial;
	}
}
