import { PrismaClient } from '@prisma/client';
import { VercelRequest, VercelResponse } from '@vercel/node';

// 서버리스 환경을 위한 Prisma 싱글톤 패턴
const globalForPrisma = globalThis as unknown as {
  prisma: PrismaClient | undefined;
};
const prisma = globalForPrisma.prisma ?? new PrismaClient();
if (process.env.NODE_ENV !== 'production') globalForPrisma.prisma = prisma;

/**
 * GET /api/leaderboard?difficulty=Easy&limit=100
 * POST /api/leaderboard  { playerName, score, maxCombo, totalCleared, difficulty }
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  // CORS 헤더 (포트폴리오 사이트에서 접근 가능하도록)
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  try {
    // POST body 파싱 (Vercel 환경에 따라 자동 파싱 안 될 수 있음)
    if (req.method === 'POST' && typeof req.body === 'string') {
      req.body = JSON.parse(req.body);
    }

    switch (req.method) {
      case 'GET':
        return await handleGet(req, res);
      case 'POST':
        return await handlePost(req, res);
      default:
        return res.status(405).json({ error: 'Method not allowed' });
    }
  } catch (error: any) {
    console.error('Leaderboard API error:', error);
    return res.status(500).json({
      error: 'Internal server error',
      message: error?.message || String(error),
      stack: process.env.NODE_ENV === 'development' ? error?.stack : undefined,
    });
  } finally {
    await prisma.$disconnect();
  }
}

async function handleGet(req: VercelRequest, res: VercelResponse) {
  const difficulty = req.query.difficulty as string | undefined;
  const limitStr = req.query.limit as string | undefined;
  const limit = Math.min(Math.max(parseInt(limitStr || '100', 10) || 100, 1), 200);

  const where = difficulty ? { difficulty } : {};

  const entries = await prisma.leaderboardEntry.findMany({
    where,
    orderBy: { score: 'desc' },
    take: limit,
    select: {
      id: true,
      playerName: true,
      score: true,
      maxCombo: true,
      totalCleared: true,
      difficulty: true,
      createdAt: true,
    },
  });

  // 순위 추가 (명시적 타입 — strict 모드 대응)
  const ranked = entries.map((entry: typeof entries[number], index: number) => ({
    ...entry,
    rank: index + 1,
  }));

  return res.status(200).json(ranked);
}

async function handlePost(req: VercelRequest, res: VercelResponse) {
  // body가 문자열인 경우 파싱
  let data: Record<string, unknown>;
  if (typeof req.body === 'string') {
    data = JSON.parse(req.body);
  } else if (req.body && typeof req.body === 'object') {
    data = req.body as Record<string, unknown>;
  } else {
    return res.status(400).json({ error: 'Invalid request body' });
  }

  const { playerName, score, maxCombo, totalCleared, difficulty, gameDuration } = data;

  // 유효성 검사
  if (!playerName || typeof playerName !== 'string' || playerName.trim().length === 0) {
    return res.status(400).json({ error: 'Invalid playerName' });
  }
  if (typeof score !== 'number' || score < 0) {
    return res.status(400).json({ error: 'Invalid score' });
  }
  if (!difficulty || !['Easy', 'Normal', 'Hard'].includes(difficulty as string)) {
    return res.status(400).json({ error: 'Invalid difficulty' });
  }

  const entry = await prisma.leaderboardEntry.create({
    data: {
      playerName: playerName.trim().slice(0, 20),
      score,
      maxCombo: Math.max(0, (maxCombo as number) || 0),
      totalCleared: Math.max(0, (totalCleared as number) || 0),
      difficulty: difficulty as string,
      gameDuration: Math.max(0, (gameDuration as number) || 0),
    },
  });

  return res.status(201).json({ id: entry.id, message: 'Score saved' });
}
