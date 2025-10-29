using Microsoft.AspNetCore.Mvc;
using MSDisTestTask.Data;
using System.Diagnostics;

namespace MSDisTestTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IDataStorage _dataStorage;
    private readonly IConfiguration _configuration;

    public HealthController(IDataStorage dataStorage, IConfiguration configuration)
    {
        _dataStorage = dataStorage;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetHealth()
    {
        try
        {
            var storageHealthResult = await GetStorageHealthStatus();
            var kafkaHealthResult = GetKafkaHealthStatus();
            
            var overallStatus = storageHealthResult.IsHealthy && kafkaHealthResult.IsHealthy 
                ? "healthy" : "degraded";

            var healthStatus = new
            {
                status = overallStatus,
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                uptime = GetUptime(),
                storage = storageHealthResult.Details,
                kafka = kafkaHealthResult.Details,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка health check: {ex.Message}");
            return StatusCode(500, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    [HttpGet("detailed")]
    public async Task<ActionResult<object>> GetDetailedHealth()
    {
        try
        {
            var storageHealthResult = await GetStorageHealthStatus();
            var kafkaHealthResult = GetKafkaHealthStatus();
            
            var overallStatus = storageHealthResult.IsHealthy && kafkaHealthResult.IsHealthy 
                ? "healthy" : "unhealthy";

            var detailedHealth = new
            {
                status = overallStatus,
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                uptime = GetUptime(),
                components = new
                {
                    storage = storageHealthResult.Details,
                    kafka = kafkaHealthResult.Details,
                    api = new
                    {
                        status = "healthy",
                        responseTime = "< 100ms"
                    }
                },
                system = new
                {
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount,
                    workingSet = GC.GetTotalMemory(false),
                    gcCollections = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    }
                }
            };

            return Ok(detailedHealth);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка получения детального health check: {ex.Message}");
            return StatusCode(500, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    private async Task<object> GetStorageHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var stats = await _dataStorage.GetUserEventStatsAsync();
            stopwatch.Stop();

            var storageType = _dataStorage.GetType().Name;
            
            return new
            {
                status = "healthy",
                type = storageType,
                responseTime = $"{stopwatch.ElapsedMilliseconds}ms",
                recordCount = stats.Count(),
                connectionString = GetConnectionInfo()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка проверки storage: {ex.Message}");
            return new
            {
                status = "unhealthy",
                type = _dataStorage.GetType().Name,
                error = ex.Message,
                connectionString = GetConnectionInfo()
            };
        }
    }

    private object GetKafkaHealth()
    {
        try
        {
            var kafkaServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "Unknown";
            var kafkaTopic = _configuration["KAFKA_TOPIC"] ?? "Unknown";
            var kafkaGroupId = _configuration["KAFKA_GROUP_ID"] ?? "Unknown";

            return new
            {
                status = "healthy",
                bootstrapServers = kafkaServers,
                topic = kafkaTopic,
                groupId = kafkaGroupId,
                note = "Kafka health is determined by consumer activity"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка проверки Kafka: {ex.Message}");
            return new
            {
                status = "unknown",
                error = ex.Message
            };
        }
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    private string GetConnectionInfo()
    {
        if (_dataStorage is PostgreSqlDataStorage)
        {
            return "PostgreSQL Database";
        }
        else if (_dataStorage is FileDataStorage)
        {
            return "File System (user_event_stats.json)";
        }
        return "Unknown Storage";
    }

    private async Task<(bool IsHealthy, object Details)> GetStorageHealthStatus()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var stats = await _dataStorage.GetUserEventStatsAsync();
            stopwatch.Stop();

            var storageType = _dataStorage.GetType().Name;
            
            var details = new
            {
                status = "healthy",
                type = storageType,
                responseTime = $"{stopwatch.ElapsedMilliseconds}ms",
                recordCount = stats.Count(),
                connectionString = GetConnectionInfo()
            };

            return (true, details);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка проверки storage: {ex.Message}");
            var details = new
            {
                status = "unhealthy",
                type = _dataStorage.GetType().Name,
                error = ex.Message,
                connectionString = GetConnectionInfo()
            };
            return (false, details);
        }
    }

    private (bool IsHealthy, object Details) GetKafkaHealthStatus()
    {
        try
        {
            var kafkaServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "Unknown";
            var kafkaTopic = _configuration["KAFKA_TOPIC"] ?? "Unknown";
            var kafkaGroupId = _configuration["KAFKA_GROUP_ID"] ?? "Unknown";

            var details = new
            {
                status = "healthy",
                bootstrapServers = kafkaServers,
                topic = kafkaTopic,
                groupId = kafkaGroupId,
                note = "Kafka health is determined by consumer activity"
            };

            return (true, details);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HealthController] Ошибка проверки Kafka: {ex.Message}");
            var details = new
            {
                status = "unknown",
                error = ex.Message
            };
            return (false, details);
        }
    }
}