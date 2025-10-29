# MSDisTestTask

Тестовое задание - сервис обработки событий пользователей из Kafka с сохранением статистики в PostgreSQL/файлы и REST API для получения данных.

## Установка и запуск

### Предварительные требования  
- Docker
- Docker Compose

### Запуск проекта
```bash
git clone <repository-url>
cd MSDisTestTask
docker-compose up -d --build
```

## Проверка эндпойнтов

### Через curl
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

### Через Postman
Импортируйте файлы в Postman:
- **Коллекция**: `MSDisTestTask.postman_collection.json` - содержит все запросы к API
- **Окружение**: `MSDisTestTask.postman_environment.json` - содержит переменные (базовый URL и порт)

## API Endpoints
- **GET /api/health** - проверка здоровья
- **GET /api/health/detailed** - детальная проверка компонентов
- **GET /api/stats** - получить статистику
- **GET /api/stats/user/{userId}** - статистика пользователя
- **GET /api/stats/users** - список пользователей
- **GET /api/stats?storage=postgresql** - статистика из PostgreSQL
- **GET /api/stats?storage=file** - статистика из файлов