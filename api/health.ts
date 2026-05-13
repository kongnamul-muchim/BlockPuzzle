import { VercelRequest, VercelResponse } from '@vercel/node';

/**
 * 헬스 체크 API — DB 없이 동작하는지 확인용
 */
export default async function handler(req: VercelRequest, res: VercelResponse) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Content-Type', 'application/json');

  return res.status(200).json({
    status: 'ok',
    timestamp: new Date().toISOString(),
    node: process.version,
    env: {
      hasDatabaseUrl: !!process.env.DATABASE_URL,
    },
  });
}
