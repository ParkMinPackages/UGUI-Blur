using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.UGUI.Blur.Components;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParkMinPackages.UGUI.Blur.RendererFeatures
{
	[ExecuteAlways, DisallowMultipleComponent]
	public sealed class BlurImageSource : MonoBehaviour
	{
		// - Public Properties -
		public BlurImageSourceMode SourceMode => _sourceMode;
		public Camera SourceCamera => _sourceCamera;
		public UnityEngine.UI.Image SourceImage => _sourceImage;
		public float Radius => _radius;
		internal RTHandle Image => _imageHandle;
		internal Texture ImageTexture => _imageTexture;

		// - Public Methods -
		internal static BlurImageSource FindFor(Camera camera) {
			BlurImageSource source = BlurImage.FindExplicitSourceFor(camera);
			source?.RefreshImageSource();
			return source;
		}

		internal bool CanRenderFor(Camera camera) {
			return IsReady() && TryGetRenderCamera(out Camera renderCamera) && renderCamera == camera;
		}
		internal bool IsReady() {
			return isActiveAndEnabled && TryGetRenderCamera(out Camera _) && (IsCameraMode || _sourceImage.sprite != null);
		}
		internal bool TryGetRenderCamera(out Camera camera) {
			if (IsCameraMode) {
				camera = _sourceCamera;
				return camera != null;
			}
			if (_sourceImage == null) {
				camera = null;
				return false;
			}

			camera = _sourceImage.canvas == null ? null : _sourceImage.canvas.rootCanvas.worldCamera;
			if (camera == null) {
				camera = Camera.main;
			}
			return camera != null;
		}

		// - Handler -
		void OnEnable() {
			RefreshImageSource();
		}
		void OnDisable() {
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
		[SerializeField] BlurImageSourceMode _sourceMode;
		[SerializeField, EnableIf(nameof(IsCameraMode))] Camera _sourceCamera;
		[SerializeField, EnableIf(nameof(IsImageMode))] UnityEngine.UI.Image _sourceImage;
		[SerializeField, Range(0f, 128f), Tooltip("Blur support radius in screen pixels.")] float _radius = 40f;
#if UNITY_EDITOR
		[SerializeField, HideInInspector] Color _applyImageColor = Color.white;
#endif

		Texture _imageTexture;
		RTHandle _imageHandle;

		bool IsCameraMode => _sourceMode == BlurImageSourceMode.Camera;
		bool IsImageMode => _sourceMode == BlurImageSourceMode.Image;

		void RefreshImageSource() {
			Texture sourceTexture = IsImageMode && _sourceImage != null && _sourceImage.sprite != null ? _sourceImage.sprite.texture : null;
			if (_imageTexture != sourceTexture) {
				_imageHandle?.Release();
				_imageTexture = sourceTexture;
				_imageHandle = sourceTexture == null ? null : RTHandles.Alloc(sourceTexture);
			}
		}

	}
}
