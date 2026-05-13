# Agents - AI 작업 규칙

> 마지막 업데이트: 2026-05-13
> 프로젝트: 10×10 블록 연계 퍼즐 (ChainCrush)
> 참조: 항상 작업 시작 전에 이 파일을 먼저 읽을 것

---

## 📋 기본 규칙

### 1. 작업 시작 전
- **이 파일(Agents.md)을 먼저 읽을 것**
- **Spec.md를 참조**하여 프로젝트 구조 파악
- 작업 전에 사용자에게 **진행 여부를 물어볼 것**
- 긍정적인 답을 받아야 작업 시작

### 2. 코드 작성 규칙
- **SOLID 원칙 준수**
- **DI(의존성 주입) 패턴 채택**
- `Assets/Core/`에는 `using UnityEngine` 절대 금지 (순수 C#만)
- Unity 스크립트는 `Assets/Unity/Adapters/`에 Adapter로만 작성
- 커밋 전에 코드 품질 검사

### 3. Git Commit 규칙
- 작업 항목 완료 시 **즉시 Commit**
- 커밋 메시지: 간결하게 (한 줄)
- 예: `Add: Grid.cs - 10×10 격자 관리 구현`

### 4. 사용 금지 도구
- **unity-cli**는 이 프로젝트에서 사용할 수 없음
- Unity Editor를 CLI로 제어하는 모든 명령은 **금지**

### 5. 문서화 규칙
- 마크다운(.md) 형식 사용
- 상태별 문서 분리:
  - `progress/` - 진행 중 문서
  - `reports/` - 완료 보고서
  - `plans/` - 계획 문서

---

## 📁 파일 참조 순서

```
0. Agents.md          ← 항상 먼저
1. Spec.md            ← 프로젝트 구조 및 시스템 스펙
2. 관련 스크립트/파일  ← 필요시 추가 참조
```

---

## 🔄 작업 단계별 행동 규범

### 계획단계
```
[사용자 요청 수신]
    ↓
[Agents.md 읽기]
    ↓
[Spec.md 읽기 → 파일 위치 확인]
    ↓
[작업 분석 및 분할]
    ↓
[사용자에게 계획 제시 + 진행 확인]
```

### 진행단계
```
[작업 실행]
    ↓
[중간중간 진행 상황 보고]
    ↓
[Git Commit - 항목별]
    ↓
[문서 업데이트]
```

### 완료단계
```
[작업 완료]
    ↓
[Git Commit]
    ↓
[완료 보고서 작성 (reports/)]
    ↓
[최종 사용자에게 보고]
```

### 오류단계
```
[문제 발생 감지]
    ↓
[문제 분석]
    ↓
[사용자에게 상황 보고]
    ↓
[해결책 제시 + 승인 후 실행]
```

---

## 🎮 게임 아키텍처 개요

### 핵심 원칙: Unity는 쉘(Shell), Core는 순수 C#
- **Core**: 모든 게임 로직은 `Assets/Core/`에 순수 C#으로 작성 (`UnityEngine` 참조 금지)
- **Unity**: `Assets/Unity/Adapters/`에 MonoBehaviour 어댑터만 작성 (렌더링, 입력, 오디오)
- **의존성 방향**: Unity → Core (단방향), Core는 Unity를 전혀 모름

```
┌──────────────────────────────┐
│  Unity Adapters (Mono)       │ ← 렌더링, 입력, 오디오 (UnityEngine 사용)
├──────────────────────────────┤
│  Core Interfaces             │ ← IGrid, IScoreManager, IGameStateMachine 등
├──────────────────────────────┤
│  Pure C# Game Logic          │ ← Grid, Block, ChainDetector, ScoreManager
└──────────────────────────────┘
```

---

## 🏗️ DI 컨테이너 아키텍처

### 개요
- **위치**: `Assets/Core/Managers/`
- **컨테이너 본체**: 순수 C# 클래스 (`MonoBehaviour` 아님)
- **초기화**: `GameManager` (`MonoBehaviour`)가 `Awake()`에서 생성 및 관리
- **접근**: `GameManager.Container` 정적 프로퍼티로 전역 접근

### IDIContainer 인터페이스

| 메서드 | 설명 |
|--------|------|
| `Register<TInterface, TImpl>(lifetime)` | 인터페이스 → 구현체 매핑 등록 |
| `Register<TImpl>(lifetime)` | 구현체 직접 등록 (인터페이스 없음) |
| `RegisterInstance<T>(instance, lifetime)` | 인스턴스 직접 등록 (주로 Singleton) |
| `Resolve<T>()` | 등록된 서비스 해결 (생성자 주입 자동 처리) |
| `CreateScope()` | Scoped 생명주기용 자식 컨테이너 생성 |
| `IsRegistered<T>()` | 특정 타입 등록 여부 확인 |

### 서비스 생명주기

| 생명주기 | 동작 | 사용 예 |
|---------|------|---------|
| `Transient` | 요청할 때마다 **새 인스턴스** 생성 | Block, ChainResult |
| `Scoped` | 같은 스코프 내에서 **인스턴스 공유** | 게임 라운드 단위 서비스 |
| `Singleton` | 전역에서 **하나의 인스턴스**만 사용 | Grid, ScoreManager, GameStateMachine |

### 의존성 주입 방식

**① 생성자 주입 (권장)** - 순수 C# 서비스에 사용
```csharp
public class Grid : IGrid
{
    private readonly IDifficultyConfig _config;
    
    public Grid(IDifficultyConfig config)
    {
        _config = config;
    }
}
```

**② GameManager.Container 직접 접근 (MonoBehaviour Adapter 한정)**
```csharp
// MonoBehaviour는 생성자 주입이 안 되므로 Adapter에서 사용
void Awake()
{
    if (GameManager.Container != null && GameManager.Container.IsRegistered<IGrid>())
    {
        _grid = GameManager.Container.Resolve<IGrid>();
    }
}
```

### 신규 서비스 등록 가이드
```
1. 인터페이스 정의: Assets/Core/Interfaces/ 에 ISomeService.cs 생성
2. 구현체 작성: 생성자 주입으로 의존성 받음
3. GameManager.RegisterCoreServices()에 등록 코드 추가
4. 사용처에서 Resolve<T>() 또는 생성자 주입으로 사용
```

### 코드 작성 시 체크리스트
- [ ] 인터페이스로 추상화했는가? (DIP)
- [ ] 의존성은 생성자로 주입받는가? (DI)
- [ ] Core 코드에 `using UnityEngine`이 없는가?
- [ ] MonoBehaviour에는 생성자 주입이 안 되므로 Adapter 패턴 활용
- [ ] 서비스 생명주기를 적절히 선택했는가?
- [ ] 등록 전에 `IsRegistered<T>()`로 중복 등록 확인할 것

---

## 📂 파일 구조

```
Assets/
├── Core/                         # 순수 C# (UnityEngine 참조 금지)
│   ├── Interfaces/               # 모든 서비스 인터페이스
│   │   ├── IGrid.cs              # 격자 관리
│   │   ├── IBlock.cs             # 블럭 데이터
│   │   ├── IChainDetector.cs     # 연계 탐지 (BFS)
│   │   ├── IScoreManager.cs      # 점수 계산
│   │   ├── IGameStateMachine.cs  # 게임 상태 전이
│   │   ├── IInputProvider.cs     # 입력 추상화
│   │   ├── IAudioService.cs      # 오디오 추상화
│   │   ├── IDifficultyConfig.cs  # 난이도 설정
│   │   └── ILeaderboardService.cs# 랭킹 API
│   ├── Game/
│   │   ├── Grid.cs               # 10×10 격자 (블럭 생성/이동/제거)
│   │   ├── Block.cs              # 블럭 (색상, 위치, 상태)
│   │   ├── BlockColor.cs         # 색상 enum 정의
│   │   ├── ChainDetector.cs      # BFS로 인접 블럭 탐색
│   │   ├── ScoreManager.cs       # 점수 계산 (연계 배율, 낙차 보너스)
│   │   ├── GameStateMachine.cs   # MainMenu → Playing → GameOver
│   │   ├── DifficultyConfig.cs   # Easy/Normal/Hard 설정
│   │   └── LeaderboardService.cs # HTTP 클라이언트로 랭킹 통신
│   ├── Managers/
│   │   ├── DIContainer.cs
│   │   └── GameManager.cs        # 유일한 MonoBehaviour in Core (부트스트래핑)
│   └── Utilities/
│       └── MathUtils.cs
│
├── Unity/                        # Unity 종속 어댑터 (UnityEngine 사용 가능)
│   ├── Adapters/
│   │   ├── UnityGridRenderer.cs     # Grid → GameObject 렌더링
│   │   ├── UnityBlockRenderer.cs    # Block → SpriteRenderer + 애니메이션
│   │   ├── UnityInputDetector.cs    # 마우스 클릭 → Core 전달
│   │   ├── UnityAudioController.cs  # 효과음 재생
│   │   └── UnityLeaderboardUI.cs    # 랭킹 표시 (WebGL/HTTP)
│   ├── ScriptableObjects/
│   │   └── DifficultySettingsSO.cs  # 난이도별 색상/속도 설정
│   ├── Prefabs/
│   │   └── BlockPrefab.prefab
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   └── GameScene.unity
│   ├── Sprites/                    # 블럭 색상별 스프라이트
│   ├── Audio/                      # 효과음 (제거, 낙하, 게임오버 등)
│   └── UI/
│       ├── GameHUD.cs              # 점수, 연계, 턴 표시
│       ├── GameOverScreen.cs       # 결과 + 이름 입력
│       └── LeaderboardUI.cs        # 랭킹 리스트
│
├── Server/                       # Vercel API (선택 사항, 별도 repo 가능)
│   ├── api/
│   │   └── leaderboard.ts
│   ├── prisma/
│   │   └── schema.prisma
│   └── package.json
```

---

## 🔍 의견/개선 제안 검증 절차

### 1. 원칙
- 의견이나 개선안 제시 전 **반드시 한 번 더 검증**할 것
- 검증되지 않은 의견은 제시하지 않음

### 2. 검증 절차
```
[의견/개선안 도출]
    ↓
[관련 코드/문서 재확인]
    ↓
[틀린 대답 여부 체크] ← 확실하지 않은 내용은 "추정" 표기
    ↓
[검증 완료 후 제시]
```

### 3. 검증 기준
| 항목 | 설명 |
|------|------|
| 사실 확인 | 관련 코드/문서를 다시 읽고 확인했는가? |
| 오류 여부 | 틀린 대답이나 오해를 불러일으킬 내용이 없는가? |
| 근거 명확성 | 제시하는 내용에 충분한 근거가 있는가? |
| 불확실성 표기 | 확실하지 않으면 "추정", "확인 필요" 등으로 명시 |

---

## 🎯 품질 기준

| 항목 | 기준 |
|------|------|
| SOLID | 5개 원칙 모두 준수 |
| DI | 의존성 명확히 분리 |
| Core 순수성 | Core 코드에 UnityEngine 참조 없음 |
| Adapter 분리 | Unity 의존 코드는 Adapter에만 |
| Commit | 항목 완료 시 즉시 |
| 문서화 | 상태 변화 시 기록 |
| 코드 리뷰 | 자기 검사 후 제출 |
| 의견 검증 | 제안 전 반드시 사실 확인 및 검증 |

---

## 📌 핵심 원칙

> "별거 아니니까 기억하지 마."
> - 항상 결과로 증명할 것
> - 유저 조작 방지를 위해 확인을 반드시 할 것
> - 문제를 조기에 발견하여 보고할 것

---

*이 문서는 AI가 작업을 수행할 때 참조하는 규칙 문서입니다.*
*프로젝트 구조 및 시스템 스펙은 Spec.md를 참고하세요.*
