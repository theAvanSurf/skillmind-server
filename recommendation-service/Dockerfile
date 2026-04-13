FROM python:3.12-slim

WORKDIR /app

# Install system dependencies first (cached layer)
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

# Install in chunks - if one layer fails, Docker replays only from that layer
# Chunk 1: Core web framework
RUN pip install --no-cache-dir --timeout=300 --retries=10 \
    fastapi==0.111.0 \
    uvicorn[standard]==0.30.1

# Chunk 2: DB layer
RUN pip install --no-cache-dir --timeout=300 --retries=10 \
    asyncpg==0.29.0 \
    sqlalchemy[asyncio]==2.0.30

# Chunk 3: Messaging & cache
RUN pip install --no-cache-dir --timeout=300 --retries=10 \
    "redis[asyncio]==5.0.4" \
    kafka-python-ng==2.2.3

# Chunk 4: Pydantic + config
RUN pip install --no-cache-dir --timeout=300 --retries=10 \
    pydantic==2.7.1 \
    pydantic-settings==2.2.1 \
    python-dotenv==1.0.1

# Chunk 5: Heavy numeric (numpy is the biggest one)
RUN pip install --no-cache-dir --timeout=300 --retries=10 \
    numpy==1.26.4

COPY . .

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--reload"]