import asyncio, json, logging, threading, time
from kafka import KafkaConsumer
from kafka.errors import NoBrokersAvailable
from app.config import settings
from app.database import AsyncSessionLocal
from app.schemas import TrackEventRequest
from app.events.processor import process_event

logger = logging.getLogger(__name__)
TOPIC = "skillmind.user.events"

async def _handle(raw: dict):
    try:
        async with AsyncSessionLocal() as db:
            await process_event(TrackEventRequest(**raw), db)
    except Exception as e:
        logger.error(f"[Kafka] Handler error: {e}")

def _loop():
    while True:
        try:
            consumer = KafkaConsumer(TOPIC, bootstrap_servers=settings.KAFKA_BROKER,
                group_id=settings.KAFKA_GROUP_ID, auto_offset_reset="earliest",
                enable_auto_commit=True, value_deserializer=lambda m: json.loads(m.decode()))
            logger.info(f"[Kafka] Listening on '{TOPIC}'")
            loop = asyncio.new_event_loop()
            for msg in consumer:
                loop.run_until_complete(_handle(msg.value))
        except NoBrokersAvailable:
            logger.warning("[Kafka] No brokers. Retrying in 5s...")
            time.sleep(5)
        except Exception as e:
            logger.error(f"[Kafka] Error: {e}. Restarting...")
            time.sleep(3)

async def start_kafka_consumer():
    threading.Thread(target=_loop, daemon=True).start()
    logger.info("[Kafka] Consumer thread started.")