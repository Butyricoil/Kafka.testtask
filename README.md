# MSDisTestTask

User event processing service that consumes events from Kafka, stores statistics in PostgreSQL/files, and provides REST API for data retrieval.

## Installation and Setup

### Prerequisites  
- Docker
- Docker Compose

### Running the Project
```bash
git clone <repository-url>
cd MSDisTestTask
docker-compose up -d --build
```

## ⚠️ Important: Creating Kafka Messages

**Don't forget to create test messages in Kafka!** The service consumes user events from Kafka topic. You need to produce messages to test the functionality.

### Sample Kafka Message Format
```json
{
  "userId": "123",
  "eventType": "login",
  "timestamp": "2024-10-30T10:00:00Z",
  "metadata": {
    "source": "web",
    "sessionId": "abc123"
  }
}
```

### Creating Test Messages
You can use Kafka CLI tools or any Kafka client to produce messages to the configured topic (check `KAFKA_TOPIC` in .env file).

Example using Kafka CLI:
```bash
docker exec -it <kafka-container-name> /bin/bash

kafka-console-producer --bootstrap-server localhost:9092 --topic user-events
```

## Testing Endpoints

### Using curl
```bash
curl http://localhost:8080/api/health
curl http://localhost:8080/api/health/detailed

curl http://localhost:8080/api/stats
curl http://localhost:8080/api/stats/user/123
curl http://localhost:8080/api/stats/users
curl "http://localhost:8080/api/stats?storage=postgresql"
curl "http://localhost:8080/api/stats?storage=file"
curl http://localhost:8080/api/stats/storage-info
```

### Using Postman
Import the following files into Postman:
- **Collection**: `MSDisTestTask.postman_collection.json` - contains all API requests
- **Environment**: `MSDisTestTask.postman_environment.json` - contains variables (base URL and port)

## API Endpoints
- **GET /api/health** - health check
- **GET /api/health/detailed** - detailed component health check
- **GET /api/stats** - get statistics
- **GET /api/stats/user/{userId}** - user statistics
- **GET /api/stats/users** - list of users
- **GET /api/stats?storage=postgresql** - statistics from PostgreSQL
- **GET /api/stats?storage=file** - statistics from files
- **GET /api/stats/storage-info** - storage information

## Environment Variables

### Kafka Configuration
```
KAFKA_BOOTSTRAP_SERVERS=localhost:9092
KAFKA_TOPIC=user-events
KAFKA_GROUP_ID=user-event-processor
```

### PostgreSQL Configuration
```
POSTGRES_DB=userdb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=password
POSTGRES_PORT=5432
POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=userdb;Username=postgres;Password=password
```

### Application Configuration
```
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
FILE_STORAGE_PATH=./user_event_stats.json
UsePostgreSQL=true
```

## Architecture

The service implements:
- **Kafka Consumer**: Consumes user events from Kafka topic
- **Observer Pattern**: Processes events using EventObservable/EventObserver
- **Dual Storage**: Supports both PostgreSQL and file-based storage
- **REST API**: Provides endpoints for statistics retrieval
- **Health Checks**: Monitors service and component health