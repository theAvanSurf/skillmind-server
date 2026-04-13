import uuid
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession
from app.database import get_db
from app.schemas import RecommendationResponse, TrackEventRequest, EventResponse
from app.engine.scorer import compute_recommendations, cold_start_recommendations
from app.events.processor import process_event
from app.redis_client import get_cached_recommendations, cache_recommendations

router = APIRouter(prefix="/recommendations", tags=["Recommendations"])

@router.get("/{profile_id}", response_model=RecommendationResponse)
async def get_recommendations(profile_id: uuid.UUID, db: AsyncSession = Depends(get_db)):
    cached = await get_cached_recommendations(str(profile_id))
    if cached:
        return RecommendationResponse(profile_id=profile_id, is_personalized=bool(cached), recommendations=cached)

    result = await compute_recommendations(profile_id, db)
    recommendations, is_personalized = result

    if not recommendations:
        recommendations = await cold_start_recommendations(db)
        is_personalized = False

    await cache_recommendations(str(profile_id), [r.model_dump(mode="json") for r in recommendations])
    return RecommendationResponse(profile_id=profile_id, is_personalized=is_personalized, recommendations=recommendations)

@router.post("/events/track", response_model=EventResponse, status_code=201)
async def track_event(event: TrackEventRequest, db: AsyncSession = Depends(get_db)):
    valid = {"started", "completed", "paused", "clicked", "searched"}
    if event.event_type not in valid:
        raise HTTPException(400, f"Invalid event_type. Must be one of: {', '.join(valid)}")
    await process_event(event, db)
    return EventResponse(success=True, message=f"Event '{event.event_type}' tracked.")

@router.get("/cold-start/trending")
async def get_trending(db: AsyncSession = Depends(get_db)):
    courses = await cold_start_recommendations(db)
    return [c.model_dump(mode="json") for c in courses]