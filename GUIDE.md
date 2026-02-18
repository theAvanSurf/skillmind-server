# SkillMind Server Setup Guide

This guide explains how to run the SkillMind server components (API Gateway and .NET Core Service) using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running.
- [Git](https://git-scm.com/) (optional, creating the project).

## Configuration

The project uses environment variables to manage sensitive configuration like database credentials and API keys.

1.  **Create the environment file**:
    Copy the provided `.env.example` file to a new file named `.env` in the root directory.

    ```bash
    cp .env.example .env
    ```

2.  **Edit the `.env` file**:
    Open the `.env` file in your text editor and fill in the values.

    *   `POSTGRES_URL`: The full connection string for your PostgreSQL database (e.g., from Render.com).
    *   `JWT_SECRET_KEY`: A strong secret key used for signing JWT tokens.
    *   `CLOUDINARY_*`: Your Cloudinary credentials for media upload.

    **Note**: The `.env` file is ignored by git to keep your secrets safe. Do not commit it.

## Running the Application

1.  **Start the services**:
    Run the following command in the root directory of the project:

    ```bash
    docker-compose up --build
    ```

    This command will:
    *   Build the Docker images for the API Gateway and the .NET Core service.
    *   Start Redis, Zookeeper, Kafka, API Gateway, and the Core service.

2.  **Access the services**:
    *   **API Gateway**: http://localhost:3000
    *   **Core Service**: http://localhost:5001 (Internal Docker port is 8080)
    *   **Redis**: `localhost:6379`
    *   **Kafka**: `localhost:9092` (Internal Docker network address is `kafka:29092`)
        *   *Note: Your applications connect here. Zookeeper is just a background requirement for Kafka.*
    *   **Zookeeper**: `localhost:2181` (Required by Kafka, usually not accessed directly)

3.  **Stopping the services**:
    Press `Ctrl+C` in the terminal where `docker-compose` is running, or run:

    ```bash
    docker-compose down
    ```

## Project Structure

-   `api-gateway/`: NestJS application acting as the API Gateway.
-   `Core/SkillmindCore/`: .NET 10.0 application containing the core business logic.
-   `docker-compose.yml`: Defines the services and their relationships.

## Troubleshooting

-   **Database Connection Errors**: Ensure the `POSTGRES_URL` in your `.env` file is correct and accessible from your network.
-   **.NET Version**: This project builds with a .NET 10.0 Preview image. If you encounter issues, ensure your Docker setup supports pulling `mcr.microsoft.com/dotnet/sdk:10.0` images.
