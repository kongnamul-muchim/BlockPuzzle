# Scene 세팅 가이드

> 작성: 2026-05-13
> 대상: Unity Editor 6000.0.x (URP 2D)

---

## 목차

1. [사전 준비](#1-사전-준비)
2. [GameScene 세팅](#2-gamescene-세팅)
3. [MainMenu Scene 세팅](#3-mainmenu-scene-세팅)
4. [프리팹 생성](#4-프리팹-생성)
5. [실행 확인](#5-실행-확인)

---

## 1. 사전 준비

### 1.1 필요 폴더 생성

Unity 에디터의 Project 창에서 다음 폴더들을 생성:

```
Assets/
├── Core/            ← (이미 있음)
├── Unity/
│   ├── Prefabs/     ← 새로 생성
│   ├── Sprites/     ← 새로 생성
│   ├── Audio/       ← 새로 생성
│   └── Scenes/      ← 새로 생성 (Scene 파일 저장용)
└── Server/          ← (선택, 필요시)
```

### 1.2 기본 Sprite 생성

블럭이 표시될 Sprite가 필요. 간단한 사각형 텍스처를 생성:

1. **Project 창 우클릭 → Create → 2D → Sprite → Square**
2. 이름: `BlockSprite`
3. **Sprite Editor** 열어서 9-slicing 불필요 (그냥 단색 사각형)
4. 위치: `Assets/Unity/Sprites/BlockSprite`

> **참고**: 런타임에 코드로 생성할 수도 있음. 아래 `BlockSpriteGenerator.cs`를 `Unity/Adapters/`에 추가하면 자동 생성됨.

### 1.3 Sprite Generator (선택)

아래 스크립트를 추가하면 별도 Sprite 없이 런타임에 텍스처 생성:

**`Assets/Unity/Adapters/BlockSpriteGenerator.cs`**
```csharp
using UnityEngine;

namespace BlockPuzzle.Unity.Adapters
{
    public static class BlockSpriteGenerator
    {
        public static Sprite CreateBlockSprite(int size = 64)
        {
            Texture2D tex = new Texture2D(size, size);
            // 흰색 사각형 + 테두리
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 테두리 (1px) = 약간 어둡게
                    if (x == 0 || x == size-1 || y == 0 || y == size-1)
                        pixels[y * size + x] = new Color(0.8f, 0.8f, 0.8f);
                    else
                        pixels[y * size + x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64);
        }
    }
}
```

---

## 2. GameScene 세팅

`Assets/Scenes/SampleScene.unity`를 활용하거나 새 Scene 생성.

### 2.1 Scene 생성

1. `Assets/Unity/Scenes/` 폴더 우클릭 → Create → Scene
2. 이름: `GameScene`
3. 더블클릭하여 열기
4. **File → Build Settings**에서 Scene 추가

### 2.2 카메라 설정

Hierarchy에서 `Main Camera` 선택:

| 속성 | 값 |
|------|-----|
| Projection | Orthographic |
| Size | 6 (10×10 그리드에 적합) |
| Background | Dark gray (#1a1a1a) |
| Position Z | -10 |

### 2.3 GameManager 설정

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `GameManager`
3. `GameManager.cs` 컴포넌트 추가 (Add Component → GameManager)

> `GameManager`는 `DontDestroyOnLoad`를 호출하므로 씬 전환 시 유지됨.

### 2.4 Grid Renderer 설정

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `GridRenderer`
3. `UnityGridRenderer` 컴포넌트 추가
4. 속성 설정:

| 속성 | 값 |
|------|-----|
| Grid Origin | X: -4.5, Y: 4.5 |
| Cell Size | 1 |
| Block Prefab | (아래 2.5에서 만든 프리팹 할당) |
| Block Sprite | `BlockSprite` (또는 생성기 사용) |

### 2.5 Block Prefab 생성

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `BlockPrefab`
3. `UnityBlockRenderer` 컴포넌트 추가
4. `SpriteRenderer` 자동 추가됨 확인
5. **Hierarchy → Project 창으로 드래그**하여 프리팹 생성
6. 위치: `Assets/Unity/Prefabs/BlockPrefab.prefab`
7. Hierarchy에서 원본 삭제

### 2.6 Input Detector 설정

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `InputDetector`
3. `UnityInputDetector` 컴포넌트 추가
4. Grid Renderer 필드에 `GridRenderer` 오브젝트 할당

### 2.7 Audio Controller 설정

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `AudioController`
3. `UnityAudioController` 컴포넌트 추가
4. Audio Clip 필드에 효과음 할당 (또는 비워두고 진행 가능)

### 2.8 Canvas + UI 설정 (필수)

#### 2.8.1 Canvas 생성

1. **Hierarchy 우클릭 → UI → Canvas**
2. 이름: `GameHUD`
3. Canvas Scaler 설정:

| 속성 | 값 |
|------|-----|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1920 x 1080 |
| Match | 0.5 |

4. Canvas에 `GameHUD.cs` 컴포넌트 추가

#### 2.8.2 HUD 텍스트 요소들

Canvas 자식으로 다음 UI 요소 생성:

```
GameHUD (Canvas)
├── ScoreText (Text)    — Position: Top-Left
├── ComboText (Text)    — Position: Top-Center
├── RemovalText (Text)  — Position: Top-Right
└── DifficultyText (Text) — Position: Top-Right (아래)
```

각 Text 요소 설정:

| 속성 | 값 |
|------|-----|
| Font | Arial (기본) |
| Font Size | 24 |
| Alignment | 적절히 |
| Color | White |

**GameHUD.cs Inspector**에서 각 Text 필드 할당.

#### 2.8.3 GameOverScreen

1. Canvas 자식으로 **Create Empty** → 이름 `GameOverScreen`
2. `GameOverScreen.cs` 컴포넌트 추가
3. 기본적으로 비활성화 (`SetActive(false)`)
4. 자식 UI 요소 구성:

```
GameOverScreen
├── FinalScoreText (Text)
├── MaxComboText (Text)
├── TotalClearedText (Text)
├── DifficultyText (Text)
├── NameInput (InputField)
├── SaveBtn (Button)     — Text: "Save & Leaderboard"
├── RetryBtn (Button)    — Text: "Retry"
└── MainMenuBtn (Button) — Text: "Main Menu"
```

#### 2.8.4 MainThreadDispatcher

1. **Hierarchy 우클릭 → Create Empty**
2. 이름: `MainThreadDispatcher`
3. `MainThreadDispatcher.cs` 컴포넌트 추가

---

## 3. MainMenu Scene 세팅

### 3.1 Scene 생성

1. `Assets/Unity/Scenes/` → Create → Scene
2. 이름: `MainMenuScene`
3. **Build Settings**에 추가 (index 0)

### 3.2 MainMenu UI

1. Canvas 생성
2. UI 요소 배치:

```
Canvas
├── TitleText (Text)    — "CHAINCRUSH" 대형 텍스트
├── EasyBtn (Button)    — Text: "Easy (4 Colors)"
├── NormalBtn (Button)  — Text: "Normal (5 Colors)"
├── HardBtn (Button)    — Text: "Hard (6 Colors)"
├── LeaderboardBtn (Button) — Text: "Leaderboard"
└── LeaderboardPanel (Panel) — LeaderboardUI (초기 비활성)
```

### 3.3 MainMenu 스크립트 (별도 추가)

**`Assets/Unity/UI/MainMenuController.cs`** (새 파일):

```csharp
using BlockPuzzle.Core.Interfaces;
using BlockPuzzle.Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockPuzzle.Unity.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _easyBtn;
        [SerializeField] private Button _normalBtn;
        [SerializeField] private Button _hardBtn;
        [SerializeField] private Button _leaderboardBtn;
        [SerializeField] private LeaderboardUI _leaderboardUI;

        private void Awake()
        {
            _easyBtn?.onClick.AddListener(() => StartGame(Difficulty.Easy));
            _normalBtn?.onClick.AddListener(() => StartGame(Difficulty.Normal));
            _hardBtn?.onClick.AddListener(() => StartGame(Difficulty.Hard));
            _leaderboardBtn?.onClick.AddListener(() => _leaderboardUI?.Open());
        }

        private void StartGame(Difficulty difficulty)
        {
            var stateMachine = GameManager.StateMachine;
            if (stateMachine != null)
            {
                stateMachine.StartGame(difficulty);
                SceneManager.LoadScene("GameScene");
            }
        }
    }
}
```

---

## 4. 프리팹 생성

### 4.1 BlockPrefab

위 2.5에서 생성 완료.

### 4.2 MainThreadDispatcher

1. `MainThreadDispatcher.cs` 컴포넌트가 붙은 GameObject를 프리팹으로 저장
2. 위치: `Assets/Unity/Prefabs/MainThreadDispatcher.prefab`

---

## 5. 실행 확인

### 5.1 Build Settings

**File → Build Settings**:

| Scene | Index |
|-------|-------|
| MainMenuScene | 0 |
| GameScene | 1 |

### 5.2 초기 실행 시나리오

1. **MainMenuScene** 로드
2. 난이도 버튼 클릭 → `GameManager.StateMachine.StartGame(difficulty)`
3. `SceneManager.LoadScene("GameScene")`
4. **GameScene**에서 `GameManager`는 DontDestroyOnLoad로 유지됨
5. `UnityGridRenderer.Awake()` → DI 컨테이너에서 `IGrid` 해결
6. Grid 초기화 → 하단 3행 블럭 표시
7. 블럭 클릭 → `UnityInputDetector` → `GameStateMachine.ProcessClick()` → 제거/낙하/점수

### 5.3 자주 발생하는 문제

| 문제 | 원인 | 해결 |
|------|------|------|
| `IGrid` not registered | GameManager가 씬에 없음 | GameManager GameObject가 씬에 있는지 확인 |
| NullReference in GridRenderer | BlockPrefab 미할당 | UnityGridRenderer Inspector에서 BlockPrefab 연결 |
| Input이 동작 안 함 | InputDetector에 GridRenderer 미참조 | UnityInputDetector의 Grid Renderer 필드 할당 |
| UI가 안 보임 | Canvas Scaler 설정 문제 | Canvas에 Scale With Screen Size 설정 |
| Scene 전환 안 됨 | Build Settings에 Scene 추가 안 함 | File → Build Settings에 Scene 등록 |

---

## 번외: 빠른 설치 런처 스크립트

아래 스크립트를 `Editor/` 폴더에 넣으면 **Tools → ChainCrush → Setup Scene** 메뉴로
기본 GameScene 구성을 자동 세팅 가능:

**`Assets/Editor/SceneSetupTool.cs`** (선택 사항, Editor 전용)

> 실제 제작 시 요청시 별도 제공 가능.
