using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParkMinPackages.UGUI.Blur.RendererFeatures
{
	[ExecuteAlways, DisallowMultipleComponent]
	public sealed class UIBlurSource : MonoBehaviour
	{
		// - Public Properties -
		public UIBlurSourceMode SourceMode => _sourceMode;
		public Camera SourceCamera => _sourceCamera;
		public UnityEngine.UI.Image SourceImage => _sourceImage;
		public float Radius => _radius;
		internal RTHandle Image => _imageHandle;
		internal Texture ImageTexture => _imageTexture;

		// - Public Methods -
		internal static UIBlurSource FindFor(Camera camera) {
			UIBlurSource source = _activeSources.LastOrDefault(activeSource => activeSource != null && activeSource.isActiveAndEnabled && activeSource.CanRenderFor(camera));
			source?.RefreshImageSource();
			return source;
		}

		// - Handler -
		void OnEnable() {
			_activeSources.Remove(this);
			_activeSources.Add(this);
			RefreshImageSource();
		}
		void OnDisable() {
			_activeSources.Remove(this);
			_imageHandle?.Release();
			_imageHandle = null;
			_imageTexture = null;
		}
#if UNITY_EDITOR
		void OnValidate() {
			RefreshImageSource();
		}
#endif

		// - Private & Protected -
		[Header(Headers.Settings)]
		[SerializeField] UIBlurSourceMode _sourceMode;
		[SerializeField, EnableIf(nameof(IsCameraMode))] Camera _sourceCamera;
		[SerializeField, EnableIf(nameof(IsImageMode))] UnityEngine.UI.Image _sourceImage;
		[SerializeField, Range(0f, 128f), Tooltip("Blur support radius in screen pixels.")] float _radius = 40f;

		Texture _imageTexture;
		RTHandle _imageHandle;

		bool IsCameraMode => _sourceMode == UIBlurSourceMode.Camera;
		bool IsImageMode => _sourceMode == UIBlurSourceMode.Image;

		bool CanRenderFor(Camera camera) {
			if (IsCameraMode) {
				return camera == _sourceCamera;
			}

			if (_sourceImage == null) {
				return false;
			}

			Camera canvasCamera = _sourceImage.canvas == null ? null : _sourceImage.canvas.rootCanvas.worldCamera;
			return canvasCamera == null ? camera == Camera.main : camera == canvasCamera;
		}
		void RefreshImageSource() {
			Texture sourceTexture = IsImageMode && _sourceImage != null && _sourceImage.sprite != null ? _sourceImage.sprite.texture : null;
			if (_imageTexture != sourceTexture) {
				_imageHandle?.Release();
				_imageTexture = sourceTexture;
				_imageHandle = sourceTexture == null ? null : RTHandles.Alloc(sourceTexture);
			}
		}

		// - Private Statics -
		static readonly List<UIBlurSource> _activeSources = new List<UIBlurSource>();
	}
}
