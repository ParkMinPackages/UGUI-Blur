# ParkMinPackages.UGUI.Blur

URP(Universal Render Pipeline) 17.x의 Render Graph를 이용해 uGUI 패널 뒤의 배경을 흐리게 표시하는 패키지입니다.

## 의존 패키지

- ParkMinPackages.Foundation 10.1.1
- ParkMinPackages.UGUI 14.0.0
- Unity UGUI 2.5.0
- Universal Render Pipeline 17.5.0

## 설정 순서

1. `Project Settings > Quality`에서 Quality Level별 URP Asset 구성을 확인합니다.
2. `ParkMinPackages/UGUI Blur/모든 Quality Level의 Renderer Assets에 Blur Renderer Feature 추가` 메뉴를 실행합니다. 모든 Quality Level의 URP Asset에 연결된 Renderer Data 전체에 Renderer Feature와 Blur Shader가 한 번에 등록됩니다. 여러 Level이 같은 URP Asset이나 Renderer Data를 공유하면 중복 설치하지 않습니다.
3. `UIBlurSource`의 Source Mode를 선택합니다.
   - `Camera`: 화면 배경을 직접 렌더링하는 Camera를 Source Camera에 연결합니다. 별도의 RenderTexture와 RawImage가 필요하지 않습니다.
   - `Image`: 화면 전체에 표시할 uGUI `Image`를 Source Image에 연결합니다.
4. 씬의 임의 GameObject에 `UIBlurSource`를 추가하고 Camera 모드에서는 Source Camera를, Image 모드에서는 Source Image를 연결합니다. 선택하지 않은 모드의 필드는 인스펙터에서 비활성화됩니다.
5. 흐린 배경을 표시할 `Image`에 `UIBlur`를 추가하고 `Runtime/Materials/M_BlurredBackground.mat`을 Material Template으로 지정합니다. `Image`의 Material 필드는 기본값으로 둡니다.
6. `UIBlur`의 Tint Color와 Opacity에서 블러 색상과 투명도를 조정합니다.

`UIBlurSource`의 Camera 모드는 Source Camera가 화면에 직접 렌더링한 색상 버퍼를 사용하므로 별도 RenderTexture나 RawImage가 필요하지 않습니다. Image 모드는 Source Image의 Sprite Texture를 직접 블러 입력으로 사용합니다. Image 모드는 전체 화면 `Simple` Image와 독립 Texture Sprite 사용을 권장합니다. `UIBlur`는 필요한 `TexCoord1` Canvas 채널과 화면 좌표 UV를 자동으로 설정합니다.

권장 계층은 다음과 같습니다.

```text
BackgroundCamera
BlurSource (UIBlurSource)
Canvas
└─ Content
   └─ BlurPanel (Image, UIBlur)
```
