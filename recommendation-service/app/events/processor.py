import json
import uuid
from datetime import datetime
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.models import UserInteractionEvent, UserBehaviorProfile
from app.schemas import TrackEventRequest
from app.redis_client import invalidate_cache

def _inc(d: dict, key: str | None, amount: int = 1):
    if key:
        d[key] = d.get(key, 0) + amount
    return d

async def process_event(event: TrackEventRequest, db: AsyncSession) -> None:
    db.add(UserInteractionEvent(
        profile_id=event.profile_id, course_id=event.course_id,
        event_type=event.event_type, category=event.category,
        tags=event.tags, search_query=event.search_query,
        engagement_seconds=event.engagement_seconds,
    ))

    result = await db.execute(select(UserBehaviorProfile).where(UserBehaviorProfile.profile_id == event.profile_id))
    profile = result.scalar_one_or_none()

    if not profile:
        profile = UserBehaviorProfile(profile_id=event.profile_id, top_categories="{}", top_tags="{}", completed_course_ids="[]", watched_course_ids="[]", total_events=0)
        db.add(profile)

    cats      = json.loads(profile.top_categories or "{}")
    tags      = json.loads(profile.top_tags or "{}")
    completed = json.loads(profile.completed_course_ids or "[]")
    watched   = json.loads(profile.watched_course_ids or "[]")
    cid       = str(event.course_id)

    if event.category:
        _inc(cats, event.category, 2 if event.event_type == "completed" else 1)

    if event.tags:
        for tag in event.tags.split(","):
            _inc(tags, tag.strip().lower())

    if event.event_type in ("started", "paused") and cid not in watched:
        watched.append(cid)

    if event.event_type == "completed" and cid not in completed:
        completed.append(cid)
        _inc(cats, event.category, 2)

    profile.top_categories       = json.dumps(cats)
    profile.top_tags             = json.dumps(tags)
    profile.completed_course_ids = json.dumps(completed[-200:])
    profile.watched_course_ids   = json.dumps(watched[-200:])
    profile.total_events         += 1
    profile.updated_at           = datetime.utcnow()

    await db.commit()
    await invalidate_cache(str(event.profile_id))