# Project Setup Instructions

To run this project, follow the steps below:

## 1. Install Dependencies
Make sure you have all the required dependencies installed for the project.

## 2. Install Docker
If you don’t have Docker installed, download and install it from [Docker Official Website](https://www.docker.com/get-started).

## 3. How to run Migrations

Generate a migration (creates a file in migrations/):

npx typeorm-ts-node-commonjs migration:generate src/infrastucture/database/migrations/Init -d src/config/data-source.ts 

Run the migration (applies it to the DB):

npx typeorm-ts-node-commonjs migration:run -d src/config/data-source.ts

## 3. Run the Project with Docker Compose
Use Docker Compose to build and start the project:

```bash
docker compose up --build
