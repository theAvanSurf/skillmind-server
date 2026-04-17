import json
import uuid
from datetime import datetime, timedelta
from typing import Optional
from sqlalchemy import select, func, desc
from sqlalchemy.ext.asyncio import AsyncSession
from app.models import Course, UserBehaviorProfile, UserInteractionEvent
from app.schemas import RecommendedCourse
from app.config import settings

def _parse(value: str):
    try:
        return json.loads(value) if value else {}
    except:
        return {}

def _tag_overlap(course_tags: str | None, user_tags: dict) -> float:
    if not course_tags or not user_tags:
        return 0.0
    course_set = {t.strip().lower() for t in course_tags.split(",")}
    total = sum(user_tags.values()) or 1
    return min(sum(user_tags.get(t, 0) for t in course_set) / total, 1.0)

async def compute_recommendations(profile_id: uuid.UUID, db: AsyncSession, limit: int = settings.RECOMMENDATIONS_LIMIT):
    result = await db.execute(select(UserBehaviorProfile).where(UserBehaviorProfile.profile_id == profile_id))
    behavior = result.scalar_one_or_none()

    if not behavior or behavior.total_events < settings.MIN_EVENTS_FOR_PERSONALIZATION:
        return await cold_start_recommendations(db), False

    top_categories = _parse(behavior.top_categories)
    top_tags       = _parse(behavior.top_tags)
    completed      = _parse(behavior.completed_course_ids)
    watched        = _parse(behavior.watched_course_ids)
    primary_cat    = max(top_categories, key=top_categories.get) if top_categories else None

    courses_result = await db.execute(select(Course).where(Course.Status == 2))
    all_courses = courses_result.scalars().all()

    cutoff = datetime.utcnow() - timedelta(days=30)
    pop_result = await db.execute(
        select(UserInteractionEvent.course_id, func.count(UserInteractionEvent.id).label("cnt"))
        .where(UserInteractionEvent.created_at >= cutoff)
        .group_by(UserInteractionEvent.course_id)
    )
    pop_map = {str(r.course_id): r.cnt for r in pop_result}
    max_pop = max(pop_map.values(), default=1)

    scored = []
    for c in all_courses:
        cid = str(c.Id)
        if cid in completed:
            continue

        cat_match  = 1.0 if (c.Category and c.Category in top_categories) else 0.0
        if c.Category == primary_cat:
            cat_match = min(cat_match * 1.5, 1.0)

        score = (
            cat_match                              * 0.40 +
            _tag_overlap(c.Tags, top_tags)         * 0.30 +
            (0.3 if cid in watched else 0.0)       * 0.15 +
            (pop_map.get(cid, 0) / max_pop)        * 0.10 +
            (0.5 if cid in watched else 0.0)       * 0.05
        )
        if score > 0:
            scored.append((score, c))

    scored.sort(key=lambda x: x[0], reverse=True)

    return [
        RecommendedCourse(
            id=c.Id, title=c.Title, thumbnail_url=c.ThumbnailUrl,
            category=c.Category, tags=c.Tags, relevance_score=round(s, 4),
            reason=f"Based on your interest in {c.Category}" if c.Category in top_categories else "Based on your recent activity"
        )
        for s, c in scored[:limit]
    ], True

async def cold_start_recommendations(db: AsyncSession, limit: int = settings.COLD_START_LIMIT):
    cutoff = datetime.utcnow() - timedelta(days=30)
    pop = await db.execute(
        select(UserInteractionEvent.course_id, func.count(UserInteractionEvent.id).label("cnt"))
        .where(UserInteractionEvent.created_at >= cutoff)
        .group_by(UserInteractionEvent.course_id).order_by(desc("cnt")).limit(limit)
    )
    ids = [r.course_id for r in pop]

    if ids:
        result = await db.execute(select(Course).where(Course.Id.in_(ids), Course.Status == 2))
    else:
        result = await db.execute(select(Course).where(Course.Status == 2).order_by(desc(Course.CreatedOn)).limit(limit))

    return [
        RecommendedCourse(id=c.Id, title=c.Title, thumbnail_url=c.ThumbnailUrl,
            category=c.Category, tags=c.Tags, relevance_score=0.0, reason="Trending on SkillMind")
        for c in result.scalars().all()
    ]