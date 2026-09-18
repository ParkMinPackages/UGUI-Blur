# ParkMinPackages.UGUI.Blur

URP(Universal Render Pipeline) 17.x의 Render Graph를 이용해 uGUI 패널 뒤의 배경을 흐리게 표시하는 패키지입니다.

## 의존 패키지

- ParkMinPackages.Foundation 10.1.1
- ParkMinPackages.UGUI 14.0.0
- Unity UGUI 2.5.0
- Universal Render Pipeline 17.5.0

## 설정 순서

1. UI 카메라가 사용하는 Universal Renderer Data에 `UIBackgroundBlurRendererFeature`를 추가합니다.
2. Renderer Feature의 Blur Shader에 `Runtime/Shaders/UIBackgroundGaussianBlur.shader`를 지정합니다.
3. 배경만 렌더링할 Camera를 만들고 프로젝트에서 정한 배경 레이어만 Culling Mask에 포함합니다.
4. UI Canvas의 가장 뒤에 화면 전체 크기의 `RawImage`를 배치합니다.
5. UI 카메라에 `UIBackgroundBlurSource`를 추가하고 Background Camera와 Background Display를 연결합니다.
6. 흐린 배경을 표시할 `Image`에 `UIBackgroundBlur`를 추가하고 `Runtime/Materials/M_BlurredBackground.mat`을 Material Template으로 지정합니다.
7. 패널별 Tint Color와 Opacity를 조정합니다.

`UIBackgroundBlurSource`는 실행 중 생성한 RenderTexture와 `RawImage.texture`, 배경 카메라의 `targetTexture` 연결을 관리하고 비활성화 시 이전 참조를 복원합니다. `UIBackgroundBlur`는 필요한 `TexCoord1` Canvas 채널과 화면 좌표 UV를 자동으로 설정합니다.

권장 계층은 다음과 같습니다.

```text
BackgroundCamera
UICamera (UIBackgroundBlurSource)
Canvas
├─ BackgroundDisplay (RawImage)
└─ Content
   └─ BlurPanel (Image, UIBackgroundBlur)
```
