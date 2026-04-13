from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    DATABASE_URL: str = "postgresql+asyncpg://user:pass@postgres:5432/skillmind_db"
    REDIS_HOST: str = "redis"
    REDIS_PORT: int = 6379
    KAFKA_BROKER: str = "kafka:29092"
    KAFKA_GROUP_ID: str = "recommendation-service"
    RECOMMENDATIONS_CACHE_TTL: int = 3600
    RECOMMENDATIONS_LIMIT: int = 20
    COLD_START_LIMIT: int = 15
    MIN_EVENTS_FOR_PERSONALIZATION: int = 3

    class Config:
        env_file = ".env"

settings = Settings()