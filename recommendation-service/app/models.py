import uuid
from typing import Optional
from datetime import datetime
from sqlalchemy import String, Integer, DateTime, ForeignKey, Text
from sqlalchemy.orm import Mapped, mapped_column
from app.database import Base

class Course(Base):
    __tablename__ = "Course"
    Id: Mapped[uuid.UUID]        = mapped_column(primary_key=True)
    Title: Mapped[str]           = mapped_column(String(200))
    Description: Mapped[str]     = mapped_column(Text)
    ThumbnailUrl: Mapped[str]    = mapped_column(String)
    Category: Mapped[Optional[str]] = mapped_column(String(100))
    Tags: Mapped[Optional[str]]     = mapped_column(String(500))
    Status: Mapped[int]          = mapped_column(Integer)
    CreatedOn: Mapped[datetime]  = mapped_column(DateTime)

class UserInteractionEvent(Base):
    __tablename__ = "UserInteractionEvent"
    id: Mapped[uuid.UUID]            = mapped_column(primary_key=True, default=uuid.uuid4)
    profile_id: Mapped[uuid.UUID]    = mapped_column()
    course_id: Mapped[uuid.UUID]     = mapped_column(ForeignKey("Course.Id", ondelete="CASCADE"))
    event_type: Mapped[str]          = mapped_column(String(50))
    category: Mapped[Optional[str]]  = mapped_column(String(100))
    tags: Mapped[Optional[str]]      = mapped_column(String(500))
    search_query: Mapped[Optional[str]] = mapped_column(String(300))
    engagement_seconds: Mapped[int]  = mapped_column(Integer, default=0)
    created_at: Mapped[datetime]     = mapped_column(DateTime, default=datetime.utcnow)

class UserBehaviorProfile(Base):
    __tablename__ = "UserBehaviorProfile"
    id: Mapped[uuid.UUID]              = mapped_column(primary_key=True, default=uuid.uuid4)
    profile_id: Mapped[uuid.UUID]      = mapped_column(unique=True)
    top_categories: Mapped[str]        = mapped_column(Text, default="{}")
    top_tags: Mapped[str]              = mapped_column(Text, default="{}")
    completed_course_ids: Mapped[str]  = mapped_column(Text, default="[]")
    watched_course_ids: Mapped[str]    = mapped_column(Text, default="[]")
    total_events: Mapped[int]          = mapped_column(Integer, default=0)
    updated_at: Mapped[datetime]       = mapped_column(DateTime, default=datetime.utcnow)