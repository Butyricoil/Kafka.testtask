# MSDisTestTask - Kafka Event Processing Service

Сервис для обработки событий пользователей из Apache Kafka с использованием паттерна Observer и сохранением статистики в PostgreSQL или файловой системе.

## 🚀 Быстрый запуск

### Предварительные требования

- Docker
- Docker Compose

### 1. Клонирование и запуск

```bash
# Клонируйте репозиторий
git clone <repository-url>
cd MSDisTestTask

# Запустите все сервисы
docker-compose up -d --build
```

### 2. Проверка статуса

```bash
# Проверьте статус контейнеров
docker-compose ps

# Все контейнеры должны быть в статусе "healthy"
```

## 📊 Тестирование

### Проверка API

```bash
# Проверка здоровья сервиса
curl http://localhost:8080/api/health

# Детальная проверка здоровья
curl http://localhost:8080/api/health/detailed

# Получить всю статистику
curl http://localhost:8080/api/stats

# Получить статистику конкретного пользователя
curl http://localhost:8080/api/stats/user/123

# Получить список всех пользователей
curl http://localhost:8080/api/stats/users

# Получить данные из PostgreSQL
curl "http://localhost:8080/api/stats?storage=postgresql"

# Получить данные из файловой системы
curl "http://localhost:8080/api/stats?storage=file"

# Информация о хранилищах
curl http://localhost:8080/api/stats/storage-info
```

### Отправка тестовых событий в Kafka

```bash
# Подключитесь к контейнеру Kafka
docker exec -it msdis-kafka bash

# Создайте продюсер для отправки сообщений
kafka-console-producer --bootstrap-server localhost:29092 --topic user-events

# Отправьте тестовые JSON сообщения:
{"userId": 123, "eventType": "click", "timestamp": "2025-04-16T12:34:56Z", "data": {"buttonId": "submit"}}
{"userId": 456, "eventType": "hover", "timestamp": "2025-04-16T12:35:00Z", "data": {"elementId": "menu"}}
{"userId": 123, "eventType": "click", "timestamp": "2025-04-16T12:35:30Z", "data": {"buttonId": "cancel"}}
```

## 📋 API Endpoints

### Health Check
- **GET /api/health** - базовая проверка здоровья
- **GET /api/health/detailed** - детальная проверка всех компонентов

### Statistics
- **GET /api/stats** - получить всю статистику
- **GET /api/stats?storage=postgresql** - статистика из PostgreSQL
- **GET /api/stats?storage=file** - статистика из файловой системы
- **GET /api/stats/user/{userId}** - статистика пользователя
- **GET /api/stats/users** - список всех пользователей
- **GET /api/stats/storage-info** - информация о хранилищах

### Примеры ответов

**Health Check:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-29T19:30:00Z",
  "version": "1.0.0",
  "uptime": "0d 2h 15m 30s",
  "storage": {
    "status": "healthy",
    "type": "PostgreSqlDataStorage",
    "responseTime": "45ms",
    "recordCount": 150
  },
  "kafka": {
    "status": "healthy",
    "bootstrapServers": "kafka:29092",
    "topic": "user-events"
  }
}
```

**Statistics:**
```json
[
  {
    "userId": 123,
    "eventType": "click",
    "count": 5
  },
  {
    "userId": 456,
    "eventType": "hover",
    "count": 3
  }
]
```

## 🏗️ Архитектура

### Компоненты системы

- **Kafka Consumer** - читает события из топика `user-events`
- **Event Observable** - реализует паттерн Observer для уведомления подписчиков
- **Event Observer** - обрабатывает события и агрегирует статистику
- **PostgreSQL Storage** - сохраняет агрегированную статистику
- **File Storage** - альтернативное файловое хранилище
- **REST API** - предоставляет доступ к статистике
- **Health Check** - мониторинг состояния всех компонентов

## 🔧 Конфигурация

### Переключение между хранилищами

Вы можете получать данные из разных хранилищ, добавив параметр `storage`:

```bash
# PostgreSQL (по умолчанию)
curl http://localhost:8080/api/stats

# Явно указать PostgreSQL
curl "http://localhost:8080/api/stats?storage=postgresql"

# Файловая система
curl "http://localhost:8080/api/stats?storage=file"
```

### Порты

- **8080** - REST API приложения
- **5432** - PostgreSQL
- **9092** - Kafka (внешний доступ)

## 🗄️ База данных

### Подключение к PostgreSQL через DataGrip

```
Host: localhost
Port: 5432
Database: events
User: postgres
Password: postgres
```

## 🛠️ Разработка

### Структура проекта

```
MSDisTestTask/
├── Controllers/
│   ├── StatsController.cs          # REST API для статистики
│   └── HealthController.cs         # Health Check API
├── Data/
│   ├── IDataStorage.cs            # Интерфейс хранилища данных
│   ├── PostgreSqlDataStorage.cs   # PostgreSQL реализация
│   └── FileDataStorage.cs         # Файловое хранилище
├── Models/
│   └── UserEvent.cs               # Модели данных
├── Services/
│   ├── KafkaConsumer.cs           # Kafka Consumer
│   ├── EventObservable.cs         # IObservable реализация
│   └── EventObserver.cs           # IObserver реализация
├── docker-compose.yml             # Docker Compose конфигурация
├── Dockerfile                     # Docker образ приложения
├── .env                          # Переменные окружения
└── Program.cs                    # Точка входа приложения
```

## 🔍 Мониторинг и отладка

### Health Check

```bash
# Базовая проверка
curl http://localhost:8080/api/health

# Детальная проверка с информацией о компонентах
curl http://localhost:8080/api/health/detailed
```

### Логи и отладка

```bash
# Детальные логи приложения
docker-compose logs -f app

# Проверка здоровья контейнеров
docker-compose ps
```

## 🎯 Особенности реализации

- **Паттерн Observer** - использует `System.Reactive` для реализации IObservable/IObserver
- **Множественные хранилища** - поддержка PostgreSQL и файловой системы
- **Health Check** - мониторинг состояния всех компонентов
- **Агрегация данных** - события агрегируются каждые 30 секунд
- **Обработка ошибок** - автоматическое переподключение к Kafka при сбоях
- **Масштабируемость** - поддержка Kafka Consumer Groups
- **Мониторинг** - подробное логирование всех операций

## 📄 Лицензия

MIT License