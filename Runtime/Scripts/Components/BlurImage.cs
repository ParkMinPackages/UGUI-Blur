using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.UGUI.Blur.RendererFeatures;
using Sirenix.OdinInspector;
using System.Collections.Generic;
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
		internal static bool HasSourceFor(Camera camera) {
			for (int i = 0; i < _activeImages.Count; i++) {
				BlurImage blurImage = _activeImages[i];
				if (blurImage != null && blurImage.isActiveAndEnabled && blurImage._source != null && blurImage._source.CanRenderFor(camera)) {
					return true;
				}
			}
			return false;
		}
		internal static void GetSourcesFor(Camera camera, List<BlurImageSource> sources) {
			sources.Clear();
			for (int i = 0; i < _activeImages.Count; i++) {
				BlurImage blurImage = _activeImages[i];
				BlurImageSource source = blurImage == null ? null : blurImage._source;
				if (blurImage != null && blurImage.isActiveAndEnabled && source != null && source.CanRenderFor(camera) && sources.Contains(source) == false) {
					source.RefreshImageSource();
					sources.Add(source);
				}
			}
		}

		// - Public Properties -
		public BlurImageSource Source
		{
			get { return _source; }
			set {
				if (_source == value) {
					return;
				}

				_source = value;
				if (graphic != null) {
					graphic.SetMaterialDirty();
				}
			}
		}

		// - Handler -
		protected override void OnEnable() {
			base.OnEnable();
			_activeImages.Remove(this);
			_activeImages.Add(this);
			MigrateLegacyTintColor();
			Canvas.preWillRenderCanvases += OnCanvasRendering;
			graphic.SetMaterialDirty();
		}
		protected override void OnDisable() {
			_activeImages.Remove(this);
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
		[SerializeField, Required, Tooltip("The source used to generate this image's blur texture. No blur is rendered when omitted.")] BlurImageSource _source;

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
				Texture outputTexture = _source == null ? null : _source.OutputTexture;
				_material.SetTexture(_texturePropertyId, outputTexture);
				_material.SetColor(_tintColorPropertyId, graphic.color);
				_material.SetFloat(_opacityPropertyId, _source != null && _source.IsReady() && outputTexture != null ? 1f : 0f);
			}
		}

		// - Private Statics -
		static readonly List<BlurImage> _activeImages = new List<BlurImage>();
		static readonly int _texturePropertyId = Shader.PropertyToID("_BlurImageTexture");
		static readonly int _tintColorPropertyId = Shader.PropertyToID("_TintColor");
		static readonly int _opacityPropertyId = Shader.PropertyToID("_Opacity");
	}
}
