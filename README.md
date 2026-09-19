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
3. `BlurImageSource`의 Source Mode를 선택합니다.
   - `Camera`: 화면 배경을 직접 렌더링하는 Camera를 Source Camera에 연결합니다. 별도의 RenderTexture와 RawImage가 필요하지 않습니다.
   - `Image`: 화면 전체에 표시할 uGUI `Image`를 Source Image에 연결합니다.
4. 씬의 임의 GameObject에 `BlurImageSource`를 추가하고 Camera 모드에서는 Source Camera를, Image 모드에서는 Source Image를 연결합니다. 선택하지 않은 모드의 필드는 인스펙터에서 비활성화됩니다.
5. 흐린 배경을 표시할 `Image`에 `BlurImage`를 추가하고 `Runtime/Materials/M_BlurredBackground.mat`을 Material Template으로 지정합니다. `Image`의 Material 필드는 기본값으로 둡니다. `BlurImage.Source`에는 사용할 `BlurImageSource`를 반드시 연결합니다.
6. `Image.color`에서 블러 위에 혼합할 색상과 혼합량을 조정합니다. RGB는 혼합할 색상이고 Alpha는 그 색상의 혼합량입니다.

`BlurImageSource`의 Camera 모드는 Source Camera가 화면에 직접 렌더링한 색상 버퍼를 사용하므로 별도 RenderTexture나 RawImage가 필요하지 않습니다. Image 모드는 Source Image의 Sprite Texture를 직접 블러 입력으로 사용합니다. Image 모드는 전체 화면 `Simple` Image와 독립 Texture Sprite 사용을 권장합니다. `BlurImage`는 필요한 `TexCoord1` Canvas 채널과 화면 좌표 UV를 자동으로 설정합니다.

`Image.color.a`를 낮추면 선명한 원본 배경이 아니라 블러된 배경이 더 많이 보입니다. 패널 전체를 페이드할 때는 `Image.color.a` 대신 `CanvasGroup.alpha`를 사용합니다. 스프라이트 알파와 uGUI 마스크·클리핑은 패널 모양을 결정하는 용도로 유지됩니다.

여러 `BlurImage`가 같은 Source를 공유할 수 있습니다. 같은 Camera를 대상으로 서로 다른 Source를 명시하면 Inspector에 경고가 표시되며, 먼저 등록된 활성 Source 하나가 해당 Camera의 전역 블러 텍스처를 생성합니다.

`BlurImageSource`의 `Utility` 영역에서 `일괄적용` 토글을 켜면 오른쪽 색상 필드의 값이 이 Source를 연결한 모든 `BlurImage`의 `Image.color`에 Editor Update마다 적용됩니다. 이 토글은 전용 Editor 인스턴스의 비직렬화 상태이므로 색상 필드를 편집해도 유지되며, Inspector를 새로 열 때마다 꺼진 상태로 시작하고 Inspector를 닫으면 적용도 중단됩니다.

`BlurImage.Source`가 비어 있거나 연결된 `BlurImageSource`가 유효하지 않으면 이전 프레임의 전역 블러 텍스처를 표시하지 않고 투명하게 렌더링됩니다. Image 모드에서는 Source Image에 Sprite가 연결되어 있어야 유효한 Source로 처리됩니다.

권장 계층은 다음과 같습니다.

```text
BackgroundCamera
BlurSource (BlurImageSource)
Canvas
└─ Content
   └─ BlurPanel (Image, BlurImage)
```
