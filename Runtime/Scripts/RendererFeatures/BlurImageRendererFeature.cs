using ParkMinPackages.Foundation.Constants;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ParkMinPackages.UGUI.Blur.RendererFeatures
{
	public sealed class BlurImageRendererFeature : ScriptableRendererFeature
	{
		// - Public Methods -
		public override void Create() {
			CoreUtils.Destroy(_material);
			_material = _blurShader == null ? null : CoreUtils.CreateEngineMaterial(_blurShader);
			_pass = new BlurPass(_material) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
			if (_material != null && renderingData.cameraData.cameraType == CameraType.Game && BlurImageSource.FindFor(renderingData.cameraData.camera) != null) {
				renderer.EnqueuePass(_pass);
			}
		}

		// - Handler -
		protected override void Dispose(bool disposing) {
			CoreUtils.Destroy(_material);
			_material = null;
			_pass = null;
		}

		// - Private & Protected -
		[Header(Headers.Required)]
		[SerializeField, Required] Shader _blurShader;

		Material _material;
		BlurPass _pass;

		// - Class Struct Enum -
		sealed class BlurPass : ScriptableRenderPass
		{
			// - Construct -
			public BlurPass(Material material) {
				_material = material;
				ConfigureInput(ScriptableRenderPassInput.Color);
				requiresIntermediateTexture = true;
			}

			// - Public Methods -
			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				BlurImageSource source = BlurImageSource.FindFor(cameraData.camera);
				if (source == null) {
					return;
				}

				// Background reduction
				TextureHandle background;
				int width;
				int height;
				TextureDimension dimension;
				int volumeDepth;
				if (source.SourceMode == BlurImageSourceMode.Camera) {
					UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
					background = resourceData.activeColorTexture;
					width = cameraData.cameraTargetDescriptor.width;
					height = cameraData.cameraTargetDescriptor.height;
					dimension = cameraData.cameraTargetDescriptor.dimension;
					volumeDepth = cameraData.cameraTargetDescriptor.volumeDepth;
				}
				else {
					Texture captured = source.ImageTexture;
					if (source.Image == null || captured == null) {
						return;
					}
					width = captured.width;
					height = captured.height;
					dimension = captured.dimension;
					volumeDepth = captured is RenderTexture renderTexture ? renderTexture.volumeDepth : 1;
					RenderTargetInfo backgroundInfo = new RenderTargetInfo { width = width, height = height, volumeDepth = volumeDepth, msaaSamples = 1, format = captured.graphicsFormat };
					background = renderGraph.ImportTexture(source.Image, backgroundInfo);
				}

				TextureDesc descriptor = new TextureDesc(width, height) { colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat, dimension = dimension, slices = volumeDepth };
				descriptor.msaaSamples = MSAASamples.None;
				descriptor.depthBufferBits = DepthBits.None;
				descriptor.clearBuffer = false;
				descriptor.filterMode = FilterMode.Bilinear;
				descriptor.width = Mathf.Max(1, descriptor.width / 2);
				descriptor.height = Mathf.Max(1, descriptor.height / 2);
				descriptor.name = "UI Background Half";
				TextureHandle half = renderGraph.CreateTexture(descriptor);
				if (source.SourceMode == BlurImageSourceMode.Camera) {
					renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(background, half, _material, 2), "UI Background Downsample Half");
				}
				else {
					using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CapturePassData>("UI Background Downsample Half", out CapturePassData passData)) {
						passData.Source = source.ImageTexture;
						passData.Material = _material;
						builder.UseTexture(background);
						builder.SetRenderAttachment(half, 0);
						builder.SetRenderFunc(static (CapturePassData data, RasterGraphContext context) => {
							MaterialPropertyBlock properties = new MaterialPropertyBlock();
							properties.SetTexture("_BlitTexture", data.Source);
							properties.SetVector("_BlitTexture_TexelSize", new Vector4(1f / data.Source.width, 1f / data.Source.height, data.Source.width, data.Source.height));
							properties.SetVector("_BlitScaleBias", new Vector4(1f, 1f, 0f, 0f));
							context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 2, MeshTopology.Triangles, 3, 1, properties);
						});
					}
				}
				descriptor.width = Mathf.Max(1, descriptor.width / 2);
				descriptor.height = Mathf.Max(1, descriptor.height / 2);
				descriptor.name = "UI Background Quarter";
				TextureHandle reduced = renderGraph.CreateTexture(descriptor);
				renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(half, reduced, _material, 2), "UI Background Downsample Quarter");

				// Separable Gaussian blur
				MaterialPropertyBlock properties = new MaterialPropertyBlock();
				properties.SetVector(_radiusId, new Vector4(source.Radius * descriptor.width / cameraData.camera.pixelWidth, source.Radius * descriptor.height / cameraData.camera.pixelHeight, 0f, 0f));
				descriptor.name = "UI Background Horizontal";
				TextureHandle horizontal = renderGraph.CreateTexture(descriptor);
				renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(reduced, horizontal, _material, 0) { propertyBlock = properties }, "UI Background Gaussian Horizontal");
				descriptor.name = "UI Background Blurred";
				TextureHandle blurred = renderGraph.CreateTexture(descriptor);
				using (IBaseRenderGraphBuilder builder = renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(horizontal, blurred, _material, 1) { propertyBlock = properties }, "UI Background Gaussian Vertical", returnBuilder: true)) {
					builder.SetGlobalTextureAfterPass(blurred, _textureId);
				}
			}

			// - Private & Protected -
			readonly Material _material;

			// - Class Struct Enum -
			sealed class CapturePassData
			{
				public Texture Source;
				public Material Material;
			}

			// - Private Statics -
			static readonly int _radiusId = Shader.PropertyToID("_BlurRadius");
			static readonly int _textureId = Shader.PropertyToID("_BlurImageTexture");
		}
	}
}
