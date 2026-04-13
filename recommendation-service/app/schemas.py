import uuid
from pydantic import BaseModel
from typing import Optional

class TrackEventRequest(BaseModel):
    profile_id: uuid.UUID
    course_id: uuid.UUID
    event_type: str
    category: Optional[str] = None
    tags: Optional[str] = None
    search_query: Optional[str] = None
    engagement_seconds: int = 0

class RecommendedCourse(BaseModel):
    id: uuid.UUID
    title: str
    thumbnail_url: str
    category: Optional[str]
    tags: Optional[str]
    relevance_score: float
    reason: Optional[str] = None

    class Config:
        from_attributes = True

class RecommendationResponse(BaseModel):
    profile_id: uuid.UUID
    is_personalized: bool
    recommendations: list[RecommendedCourse]

class EventResponse(BaseModel):
    success: bool
    message: str