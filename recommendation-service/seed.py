import asyncio
import uuid
import random
from datetime import datetime, timedelta
from app.database import AsyncSessionLocal, engine, Base
from app.models import Course, UserInteractionEvent, UserBehaviorProfile
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

async def seed_data():
    logger.info("Connecting to database...")
    async with engine.begin() as conn:
        # Note: Do not drop tables if they are managed by EF Core migrations!
        # Just create them if they somehow don't exist.
        await conn.run_sync(Base.metadata.create_all)
        
    async with AsyncSessionLocal() as session:
        # 1. Create realistic courses
        logger.info("Generating courses...")
        categories_tags = {
            "Web Dev": ["React", "Next.js", "Frontend", "JavaScript", "HTML/CSS"],
            "Backend": ["Node.js", "Python", ".NET", "C#", "SQL", "Architecture"],
            "Mobile": ["React Native", "iOS", "SwiftUI", "Android"],
            "AI & ML": ["Deep Learning", "TensorFlow", "OpenAI", "Data Science"],
            "Design": ["Figma", "UI/UX", "Typography", "Colors"],
            "DevOps": ["Docker", "Kubernetes", "AWS", "CI/CD", "Linux"]
        }

        course_images = [
            "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=600&q=80",
            "https://images.unsplash.com/photo-1633356122544-f134324a6cee?auto=format&fit=crop&w=600&q=80",
            "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?auto=format&fit=crop&w=600&q=80",
            "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?auto=format&fit=crop&w=600&q=80",
            "https://images.unsplash.com/photo-1677442136019-21780ecad995?auto=format&fit=crop&w=600&q=80"
        ]

        created_courses = []
        for cat, tags in categories_tags.items():
            for i in range(1, 4):  # 3 courses per category
                course = Course(
                    Id=uuid.uuid4(),
                    Title=f"{cat} Mastery: Course {i}",
                    Description=f"An in-depth look at building production-ready apps using {tags[0]} and {tags[1]}.",
                    ThumbnailUrl=random.choice(course_images),
                    Category=cat,
                    Tags=",".join(random.sample(tags, min(2, len(tags)))),
                    Status=1, # 1 for Published/Active
                    CreatedOn=datetime.utcnow() - timedelta(days=random.randint(10, 100))
                )
                created_courses.append(course)
                session.add(course)
        
        await session.commit()
        logger.info(f"Successfully added {len(created_courses)} courses to the database!")

        # 2. Simulate User Behavior Events over the last 30 days
        logger.info("Simulating user traffic...")
        dummy_profile_ids = [uuid.uuid4() for _ in range(5)]
        
        for _ in range(250):
            # Pick a random profile and a random course
            profile_id = random.choice(dummy_profile_ids)
            course = random.choice(created_courses)
            
            # Weighted random event
            event_type = random.choices(
                ["clicked", "started", "paused", "completed", "searched"],
                weights=[40, 30, 15, 10, 5]
            )[0]
            
            event = UserInteractionEvent(
                id=uuid.uuid4(),
                profile_id=profile_id,
                course_id=course.Id,
                event_type=event_type,
                category=course.Category,
                tags=course.Tags,
                engagement_seconds=random.randint(10, 1200) if event_type in ["started", "paused"] else 0,
                created_at=datetime.utcnow() - timedelta(days=random.randint(0, 30), hours=random.randint(1, 23))
            )
            session.add(event)
        
        await session.commit()
        logger.info("Successfully seeded Engine Trending Data Events!")
        
        logger.info("Done! Your recommendation engine is now fully populated.")

if __name__ == "__main__":
    asyncio.run(seed_data())
