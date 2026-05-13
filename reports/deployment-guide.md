# ChainCrush 배포 가이드

> 목표: 리더보드 API (Vercel + Neon) + WebGL 빌드 (Unity) → 포트폴리오 사이트에 데모 업로드

---

## 1. 리더보드 서버 배포 (Vercel + Neon)

### 1.1 사전 준비

필요한 계정:
- [Vercel](https://vercel.com) 계정 (GitHub 로그인 가능)
- [Neon](https://neon.tech) 계정 (PostgreSQL, 서버리스, 무료 티어 있음)

### 1.2 Neon DB 생성

1. [Neon Console](https://console.neon.tech) → **Create Project**
2. 프로젝트명: `chaincrush`
3. 리전: 가까운 곳 (도쿄/싱가포르 추천)
4. 생성 후 **Connection string** 복사
   ```
   postgresql://user:pass@ep-xxxx.us-east-2.aws.neon.tech/chaincrush?sslmode=require
   ```

### 1.3 Vercel 프로젝트 생성

```bash
cd Server/

# Vercel CLI 설치
npm i -g vercel

# Vercel 로그인
vercel login

# 프로젝트 생성
vercel --prod
```

- 로그인 후 프롬프트 따라감
- `Set up and deploy?` → `Y`
- `Which scope?` → 개인 계정
- `Link to existing project?` → `N` (새 프로젝트)
- `Project name:` → `chaincrush-leaderboard`
- `Directory:` → `.` (Server 폴더)
- `Override settings?` → `N`

### 1.4 환경 변수 설정

Vercel Dashboard → chaincrush-leaderboard → **Settings → Environment Variables**:

| Name | Value |
|------|-------|
| `DATABASE_URL` | (Neon connection string) |

또는 CLI로:
```bash
vercel env add DATABASE_URL
# → Neon connection string 입력
# → Production에 추가
```

### 1.5 Prisma 마이그레이션

```bash
# 로컬에서 DB 스키마 동기화
npm install
npx prisma generate
npx prisma db push  # 첫 배포는 push로 충분
```

### 1.6 재배포

```bash
vercel --prod
```

배포 완료 후 URL 확인:
```
https://chaincrush-leaderboard.vercel.app/api/leaderboard
```

### 1.7 테스트

```bash
# POST 테스트
curl -X POST https://chaincrush-leaderboard.vercel.app/api/leaderboard \
  -H "Content-Type: application/json" \
  -d '{"playerName":"Test","score":100,"maxCombo":3,"totalCleared":10,"difficulty":"Easy"}'

# GET 테스트
curl https://chaincrush-leaderboard.vercel.app/api/leaderboard
```

---

## 2. Unity WebGL 빌드

### 2.1 Unity 설정 변경

**Player Settings** (File → Build Settings → Player Settings):

| 항목 | 설정값 |
|------|--------|
| **Company Name** | (본인 이름/브랜드) |
| **Product Name** | `ChainCrush` |
| **Default Icon** | (게임 아이콘, 없으면 기본 사용) |

**Resolution and Presentation**:

| 항목 | 설정값 |
|------|--------|
| **Run in Background** | `true` 체크 |
| **WebGL Template** | `Default` (또는 원하는 템플릿) |

**Publishing Settings**:

| 항목 | 설정값 |
|------|--------|
| **Compression Format** | `Gzip` (권장) |

### 2.2 API URL 설정

1. Unity Editor에서 `GameScene` 열기
2. Hierarchy → `LeaderboardService` GameObject 선택
3. Inspector → `UnityLeaderboardService` 컴포넌트
4. **API Base URL**에 본인 Vercel URL 입력:
   ```
   https://chaincrush-leaderboard.vercel.app/api
   ```

### 2.3 씬에 LeaderboardService 추가

GameScene에 LeaderboardService가 없으면 추가:

1. Hierarchy 우클릭 → Create Empty
2. 이름: `LeaderboardService`
3. `UnityLeaderboardService` 컴포넌트 추가
4. API Base URL 입력

MainMenuScene에도 동일하게 추가 (LeaderboardUI에서 API 호출).

### 2.4 Build

1. **File → Build Settings**
2. Platform: **WebGL** 선택 → **Switch Platform**
3. **Player Settings** 확인
4. **Build** 버튼
5. 출력 폴더: `Builds/WebGL/` (또는 원하는 위치)
6. 빌드 완료 후 `Builds/WebGL/` 폴더에 index.html + wasm + data 파일들 생성됨

---

## 3. 포트폴리오 사이트 업로드

### 3.1 GitHub Pages / Vercel / Netlify 등

빌드된 `Builds/WebGL/` 폴더를 통째로 호스팅:

**Vercel에 업로드 (권장):**
```bash
cd Builds/WebGL/
vercel --prod
```

**GitHub Pages:**
1. `Builds/WebGL/`을 `docs/` 폴더로 복사
2. GitHub repo에 push
3. Settings → Pages → Source: `docs/` 폴더

**직접 iframe 임베드:**
```html
<iframe src="https://chaincrush.vercel.app" 
  width="800" height="650" 
  style="border: none; border-radius: 8px;">
</iframe>
```

### 3.2 포트폴리오에 추가 예시

```
## ChainCrush - 10×10 블록 퍼즐

[게임 플레이하기](https://chaincrush.vercel.app)

- 순수 C# 코어 로직 + Unity 렌더링 분리 아키텍처
- DI 컨테이너를 통한 의존성 주입
- BFS 기반 연쇄 제거 시스템
- Vercel Serverless + PostgreSQL 리더보드
```

---

## 4. 문제 해결

| 문제 | 원인 | 해결 |
|------|------|------|
| API 504 에러 | Neon DB가 cold start 중 | 첫 요청은 5~10초 정도 걸림 (정상) |
| CORS 에러 | Vercel API CORS 헤더 확인 | `api/leaderboard.ts`에 `Access-Control-Allow-Origin: *` 이미 있음 |
| WebGL 빌드 안 됨 | Unity 버전 문제 | Unity 6000.0.x WebGL 모듈 설치 확인 |
| 리더보드 저장 안 됨 | API URL 잘못됨 | Unity Inspector에서 `_apiBaseUrl` 확인 |
| "Entry not found" | DB 테이블 없음 | `npx prisma db push` 실행 |
