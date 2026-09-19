using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
		internal RTHandle Output => _outputHandle;
		internal Texture OutputTexture => _outputHandle == null ? null : _outputHandle.rt;

		// - Public Methods -
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
		internal void RefreshImageSource() {
			Texture sourceTexture = IsImageMode && _sourceImage != null && _sourceImage.sprite != null ? _sourceImage.sprite.texture : null;
			if (_imageTexture != sourceTexture) {
				_imageHandle?.Release();
				_imageTexture = sourceTexture;
				_imageHandle = sourceTexture == null ? null : RTHandles.Alloc(sourceTexture);
			}
		}
		internal void EnsureOutput(int width, int height, GraphicsFormat graphicsFormat, TextureDimension dimension, int volumeDepth) {
			RenderTextureDescriptor descriptor = new RenderTextureDescriptor(Mathf.Max(1, width), Mathf.Max(1, height)) {
				graphicsFormat = graphicsFormat,
				depthStencilFormat = GraphicsFormat.None,
				msaaSamples = 1,
				dimension = dimension,
				volumeDepth = Mathf.Max(1, volumeDepth),
				useMipMap = false,
				autoGenerateMips = false
			};
			RenderingUtils.ReAllocateHandleIfNeeded(ref _outputHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurImageTexture_" + GetEntityId());
		}

		// - Handler -
		void OnEnable() {
			RefreshImageSource();
		}
		void OnDisable() {
			ReleaseResources();
		}
		void OnDestroy() {
			ReleaseResources();
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
		RTHandle _outputHandle;

		bool IsCameraMode => _sourceMode == BlurImageSourceMode.Camera;
		bool IsImageMode => _sourceMode == BlurImageSourceMode.Image;

		void ReleaseResources() {
			_imageHandle?.Release();
			_imageHandle = null;
			_imageTexture = null;
			_outputHandle?.Release();
			_outputHandle = null;
		}
	}
}
