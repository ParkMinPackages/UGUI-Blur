using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.UGUI.Blur.Components;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
			if (_material != null && renderingData.cameraData.cameraType == CameraType.Game && BlurImage.HasSourceFor(renderingData.cameraData.camera)) {
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
				BlurImage.GetSourcesFor(cameraData.camera, _sources);
				for (int i = 0; i < _sources.Count; i++) {
					RecordSource(renderGraph, frameData, cameraData, _sources[i]);
				}
			}

			// - Private & Protected -
			readonly Material _material;
			readonly List<BlurImageSource> _sources = new List<BlurImageSource>();

			void RecordSource(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData, BlurImageSource source) {
				// Background reduction
				TextureHandle background;
				TextureDesc descriptor;
				int sourceWidth;
				int sourceHeight;
				if (source.SourceMode == BlurImageSourceMode.Camera) {
					UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
					background = resourceData.activeColorTexture;
					descriptor = background.GetDescriptor(renderGraph);
					sourceWidth = descriptor.width;
					sourceHeight = descriptor.height;
				}
				else {
					Texture captured = source.ImageTexture;
					if (source.Image == null || captured == null) {
						return;
					}

					sourceWidth = captured.width;
					sourceHeight = captured.height;
					TextureDimension dimension = captured.dimension;
					int volumeDepth = captured is RenderTexture renderTexture ? renderTexture.volumeDepth : 1;
					RenderTargetInfo backgroundInfo = new RenderTargetInfo { width = sourceWidth, height = sourceHeight, volumeDepth = volumeDepth, msaaSamples = 1, format = captured.graphicsFormat };
					background = renderGraph.ImportTexture(source.Image, backgroundInfo);
					descriptor = new TextureDesc(sourceWidth, sourceHeight) { colorFormat = captured.graphicsFormat, dimension = dimension, slices = volumeDepth };
				}

				descriptor.colorFormat = SystemInfo.GetCompatibleFormat(descriptor.colorFormat, FormatUsage.Render);
				descriptor.msaaSamples = MSAASamples.None;
				descriptor.depthBufferBits = DepthBits.None;
				descriptor.clearBuffer = false;
				descriptor.filterMode = FilterMode.Bilinear;
				descriptor.width = Mathf.Max(1, sourceWidth / 2);
				descriptor.height = Mathf.Max(1, sourceHeight / 2);
				descriptor.name = "Blur Image Half " + source.GetEntityId();
				TextureHandle half = renderGraph.CreateTexture(descriptor);
				if (source.SourceMode == BlurImageSourceMode.Camera) {
					renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(background, half, _material, 2), "Blur Image Downsample Half");
				}
				else {
					using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CapturePassData>("Blur Image Downsample Half", out CapturePassData passData)) {
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
				descriptor.name = "Blur Image Quarter " + source.GetEntityId();
				TextureHandle reduced = renderGraph.CreateTexture(descriptor);
				renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(half, reduced, _material, 2), "Blur Image Downsample Quarter");

				// Separable Gaussian blur
				MaterialPropertyBlock properties = new MaterialPropertyBlock();
				properties.SetVector(_radiusId, new Vector4(source.Radius * descriptor.width / sourceWidth, source.Radius * descriptor.height / sourceHeight, 0f, 0f));
				descriptor.name = "Blur Image Horizontal " + source.GetEntityId();
				TextureHandle horizontal = renderGraph.CreateTexture(descriptor);
				renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(reduced, horizontal, _material, 0) { propertyBlock = properties }, "Blur Image Gaussian Horizontal");

				source.EnsureOutput(descriptor.width, descriptor.height, descriptor.colorFormat, descriptor.dimension, descriptor.slices);
				TextureHandle blurred = renderGraph.ImportTexture(source.Output);
				renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(horizontal, blurred, _material, 1) { propertyBlock = properties }, "Blur Image Gaussian Vertical");
			}

			// - Class Struct Enum -
			sealed class CapturePassData
			{
				public Texture Source;
				public Material Material;
			}

			// - Private Statics -
			static readonly int _radiusId = Shader.PropertyToID("_BlurRadius");
		}
	}
}
