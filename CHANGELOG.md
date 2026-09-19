# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-09-19

### Changed
- Renamed the public APIs to `BlurImage`, `BlurImageSource`, `BlurImageSourceMode`, and `BlurImageRendererFeature`.
- Renamed the global blur texture property to `_BlurImageTexture`.
- Removed the separate `BlurImage` Tint Color and Opacity controls.
- Changed `UnityEngine.UI.Image.color` so RGB selects the color blended over the blurred background and Alpha controls only that tint blend amount.
- Kept the blurred panel output opaque so lowering the Image Alpha no longer reveals the sharp source background.
- Added one-time migration from the previously serialized Tint Color to `Image.color` while preserving CanvasGroup-driven fades.

## [2.0.0] - 2026-09-18

### Added
- Added Camera and Image source modes that do not require a separate RenderTexture display.
- Added an Editor menu that installs `UIBlurRendererFeature` and its Blur Shader into every unique Renderer Data assigned across all Quality Level URP Assets in one operation.

### Changed
- Renamed the runtime APIs to `UIBlur`, `UIBlurSource`, `UIBlurSourceMode`, and `UIBlurRendererFeature`.
- Changed `UIBlurSource` so it can be placed on any GameObject and selects either a Camera or uGUI Image as its source.
- Changed the Renderer Feature and UI material to use the `_UIBlurTexture` global texture.
- Changed the source inspector to keep both source fields visible while disabling the field that does not match the selected mode.
- Clarified that the target `Image` should keep its Material field at the default value.

## [1.0.0] - 2026-09-18

### Added
- Added URP 17.x Render Graph background capture and separable Gaussian blur.
- Added UIBackgroundBlurSource for managing the background RenderTexture and display.
- Added UIBackgroundBlur for panel-local tint, opacity, and screen-space blur sampling.
- Added the default blur shaders and material.
