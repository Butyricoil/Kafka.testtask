using Microsoft.AspNetCore.Mvc;
using MSDisTestTask.Data;
using MSDisTestTask.Models;

namespace MSDisTestTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public StatsController(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserEventStats>>> GetStats([FromQuery] string? storage = null)
    {
        try
        {
            var dataStorage = GetDataStorage(storage);
            Console.WriteLine($"[StatsController] Запрос статистики событий пользователей через {dataStorage.GetType().Name}");
            var stats = await dataStorage.GetUserEventStatsAsync();
            Console.WriteLine($"[StatsController] Возвращено {stats.Count()} записей статистики");
            return Ok(stats);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StatsController] Ошибка получения статистики: {ex.Message}");
            return StatusCode(500, new { error = "Ошибка получения статистики", details = ex.Message });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<UserEventStats>>> GetUserStats(int userId, [FromQuery] string? storage = null)
    {
        try
        {
            var dataStorage = GetDataStorage(storage);
            Console.WriteLine($"[StatsController] Запрос статистики для пользователя: {userId} через {dataStorage.GetType().Name}");
            var allStats = await dataStorage.GetUserEventStatsAsync();
            var userStats = allStats.Where(s => s.UserId == userId).ToList();
            Console.WriteLine($"[StatsController] Найдено {userStats.Count} записей для пользователя {userId}");
            return Ok(userStats);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StatsController] Ошибка получения статистики пользователя: {ex.Message}");
            return StatusCode(500, new { error = "Ошибка получения статистики пользователя", details = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<int>>> GetUsers([FromQuery] string? storage = null)
    {
        try
        {
            var dataStorage = GetDataStorage(storage);
            Console.WriteLine($"[StatsController] Запрос списка пользователей через {dataStorage.GetType().Name}");
            var stats = await dataStorage.GetUserEventStatsAsync();
            var users = stats.Select(s => s.UserId).Distinct().OrderBy(id => id).ToList();
            Console.WriteLine($"[StatsController] Найдено {users.Count} уникальных пользователей");
            return Ok(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StatsController] Ошибка получения списка пользователей: {ex.Message}");
            return StatusCode(500, new { error = "Ошибка получения списка пользователей", details = ex.Message });
        }
    }

    [HttpGet("storage-info")]
    public ActionResult<object> GetStorageInfo()
    {
        try
        {
            var currentStorage = _serviceProvider.GetRequiredService<IDataStorage>();
            var storageInfo = new
            {
                currentStorage = currentStorage.GetType().Name,
                availableStorages = new[] { "postgresql", "file" },
                usage = new
                {
                    postgresql = "?storage=postgresql - использовать PostgreSQL",
                    file = "?storage=file - использовать файловую систему",
                    @default = "без параметра - использовать текущее хранилище"
                }
            };
            return Ok(storageInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StatsController] Ошибка получения информации о хранилище: {ex.Message}");
            return StatusCode(500, new { error = "Ошибка получения информации о хранилище", details = ex.Message });
        }
    }

    private IDataStorage GetDataStorage(string? storageType)
    {
        return storageType?.ToLower() switch
        {
            "postgresql" or "postgres" or "pg" => _serviceProvider.GetRequiredService<PostgreSqlDataStorage>(),
            "file" or "filesystem" or "json" => _serviceProvider.GetRequiredService<FileDataStorage>(),
            _ => _serviceProvider.GetRequiredService<IDataStorage>()
        };
    }
}