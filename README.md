# ChainCrush - 10×10 블록 연계 퍼즐

> 순수 C# Core + Unity Shell + DI 컨테이너 아키텍처

## 🎮 데모

**👉 https://kongnamul-muchim.github.io/BlockPuzzle/**

## 🏗️ 아키텍처

```
Unity Layer (MonoBehaviour)  ← 렌더링, 입력, 오디오
         ↓
Core Interfaces              ← IGrid, IScoreManager, IGameStateMachine...
         ↓
Pure C# Game Logic           ← Grid, Block, ChainDetector, ScoreManager
```

- **Core**: `Assets/Core/` — 순수 C#, `using UnityEngine` 금지
- **Unity**: `Assets/Unity/Adapters/` — MonoBehaviour 어댑터만
- **DI**: DIContainer로 모든 서비스 생성자 주입

## 🎯 게임 규칙

- 10×10 격자에서 같은 색 블럭 2개 이상 클릭하여 제거
- 3회 제거 시 하단에 새 블럭 행 추가
- 블럭이 상단을 넘어가면 게임 오버
- 난이도별 4/5/6색 (Easy/Normal/Hard)

## 🚀 로컬 실행

1. Unity 6000.0.x (URP 2D)로 프로젝트 열기
2. `MainMenuScene` 열기 → Play
3. 또는 `File → Build Settings → WebGL → Build`

## 🌐 배포

### WebGL (GitHub Pages)

1. Unity에서 `Builds/WebGL/` 폴더로 WebGL 빌드
2. `git add Builds/WebGL/ && git commit -m "WebGL build" && git push`
3. GitHub Actions가 자동으로 GitHub Pages에 배포

### Leaderboard API (Vercel + Neon)

1. [Neon](https://neon.tech) → PostgreSQL 생성 → `DATABASE_URL` 복사
2. Vercel에 `Server/` 폴더 배포 → 환경변수에 `DATABASE_URL` 등록
3. Unity → `LeaderboardService` → `_apiBaseUrl`에 Vercel URL 입력
4. 재빌드 후 재배포

## 📁 프로젝트 구조

```
Assets/
├── Core/                         # 순수 C# 게임 로직
│   ├── Interfaces/               # 9개 서비스 인터페이스
│   ├── Game/                     # Grid, Block, ChainDetector, ScoreManager...
│   ├── Managers/                 # DIContainer, GameManager
│   └── Utilities/                # GameplayLogger
├── Unity/                        # Unity 어댑터
│   ├── Adapters/                 # Grid/Block/Input/Audio/Leaderboard 렌더러
│   ├── UI/                       # GameHUD, GameOverScreen, LeaderboardUI...
│   ├── Scenes/                   # MainMenuScene, GameScene
│   ├── Prefabs/                  # BlockPrefab, EntryText...
│   └── Sprites/                  # BlockSprite
├── Server/                       # Vercel Serverless API
│   ├── api/leaderboard.ts        # GET/POST 엔드포인트
│   └── prisma/schema.prisma      # PostgreSQL 스키마
└── .github/workflows/            # GitHub Actions (자동 Pages 배포)
```
