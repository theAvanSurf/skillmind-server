import json
import redis.asyncio as aioredis
from app.config import settings

_redis = None

async def get_redis():
    global _redis
    if _redis is None:
        _redis = aioredis.Redis(host=settings.REDIS_HOST, port=settings.REDIS_PORT, decode_responses=True)
    return _redis

async def cache_recommendations(profile_id: str, data: list) -> None:
    r = await get_redis()
    await r.setex(f"recommendations:{profile_id}", settings.RECOMMENDATIONS_CACHE_TTL, json.dumps(data))

async def get_cached_recommendations(profile_id: str):
    r = await get_redis()
    raw = await r.get(f"recommendations:{profile_id}")
    return json.loads(raw) if raw else None

async def invalidate_cache(profile_id: str) -> None:
    r = await get_redis()
    await r.delete(f"recommendations:{profile_id}")