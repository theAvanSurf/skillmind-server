import asyncio
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.database import engine, Base
from app.routes.recommendations import router as recommendations_router
from app.events.kafka_consumer import start_kafka_consumer

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@asynccontextmanager
async def lifespan(app: FastAPI):
    for attempt in range(1, 11):
        try:
            async with engine.begin() as conn:
                await conn.run_sync(Base.metadata.create_all)
            break
        except Exception as e:
            if attempt == 10:
                raise
            logger.warning("[DB] Connect failed (attempt %d/10): %s — retrying in 3s", attempt, e)
            await asyncio.sleep(3)
    logger.info("[DB] Tables ready.")
    await start_kafka_consumer()
    yield
    await engine.dispose()

app = FastAPI(
    title="SkillMind Recommendation Engine",
    version="1.0.0",
    lifespan=lifespan,
)

app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])
app.include_router(recommendations_router)

@app.get("/health")
async def health():
    return {"status": "ok", "service": "recommendation-engine"}