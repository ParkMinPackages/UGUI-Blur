using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace ParkMinPackages.UGUI.Blur.RendererFeatures
{
	[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
	public sealed class UIBackgroundBlurSource : MonoBehaviour
	{
		// - Public Properties -
		public float Radius => _radius;
		internal RTHandle Background => _backgroundHandle;
		internal RenderTexture BackgroundTexture => _backgroundTexture;

		// - Handler -
		void OnEnable() {
			_camera = GetComponent<Camera>();
			if (_backgroundCamera != null && _backgroundDisplay != null) {
				_previousTarget = _backgroundCamera.targetTexture;
				_previousTexture = _backgroundDisplay.texture;
				PrepareBackground();
				RenderPipelineManager.beginCameraRendering += OnCameraRendering;
			}
		}
		void OnDisable() {
			RenderPipelineManager.beginCameraRendering -= OnCameraRendering;
			if (_backgroundCamera != null && _backgroundCamera.targetTexture == _backgroundTexture) {
				_backgroundCamera.targetTexture = _previousTarget;
			}
			if (_backgroundDisplay != null && _backgroundDisplay.texture == _backgroundTexture) {
				_backgroundDisplay.texture = _previousTexture;
			}
			_backgroundHandle?.Release();
			_backgroundHandle = null;
			CoreUtils.Destroy(_backgroundTexture);
			_backgroundTexture = null;
		}
		void OnCameraRendering(ScriptableRenderContext context, Camera camera) {
			if (camera == _backgroundCamera) {
				PrepareBackground();
			}
		}

		// - Private & Protected -
		[Header(Headers.Required)]
		[SerializeField, Required] Camera _backgroundCamera;
		[SerializeField, Required] UnityEngine.UI.RawImage _backgroundDisplay;

		[Header(Headers.Settings)]
		[SerializeField, Range(0f, 128f), Tooltip("Blur support radius in screen pixels.")] float _radius = 40f;

		Camera _camera;
		RenderTexture _backgroundTexture;
		RTHandle _backgroundHandle;
		RenderTexture _previousTarget;
		Texture _previousTexture;

		void PrepareBackground() {
			int width = Mathf.Max(1, _camera.pixelWidth);
			int height = Mathf.Max(1, _camera.pixelHeight);
			if (_backgroundTexture == null || _backgroundTexture.width != width || _backgroundTexture.height != height) {
				_backgroundHandle?.Release();
				CoreUtils.Destroy(_backgroundTexture);
				_backgroundTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "UI Background Capture", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
				_backgroundTexture.Create();
				_backgroundHandle = RTHandles.Alloc(new RenderTargetIdentifier(_backgroundTexture));
				_backgroundCamera.targetTexture = _backgroundTexture;
				_backgroundDisplay.texture = _backgroundTexture;
			}
		}
	}
}
