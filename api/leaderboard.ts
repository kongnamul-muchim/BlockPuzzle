import { PrismaClient } from '@prisma/client';
import { VercelRequest, VercelResponse } from '@vercel/node';

const prisma = new PrismaClient();

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
    switch (req.method) {
      case 'GET':
        return await handleGet(req, res);
      case 'POST':
        return await handlePost(req, res);
      default:
        return res.status(405).json({ error: 'Method not allowed' });
    }
  } catch (error) {
    console.error('Leaderboard API error:', error);
    return res.status(500).json({ error: 'Internal server error' });
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
  const { playerName, score, maxCombo, totalCleared, difficulty } = req.body;

  // 유효성 검사
  if (!playerName || typeof playerName !== 'string' || playerName.trim().length === 0) {
    return res.status(400).json({ error: 'Invalid playerName' });
  }
  if (typeof score !== 'number' || score < 0) {
    return res.status(400).json({ error: 'Invalid score' });
  }
  if (!difficulty || !['Easy', 'Normal', 'Hard'].includes(difficulty)) {
    return res.status(400).json({ error: 'Invalid difficulty' });
  }

  const entry = await prisma.leaderboardEntry.create({
    data: {
      playerName: playerName.trim().slice(0, 20),
      score,
      maxCombo: Math.max(0, maxCombo || 0),
      totalCleared: Math.max(0, totalCleared || 0),
      difficulty,
      gameDuration: Math.max(0, req.body.gameDuration || 0),
    },
  });

  return res.status(201).json({ id: entry.id, message: 'Score saved' });
}
