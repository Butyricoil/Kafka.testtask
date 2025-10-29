using Microsoft.AspNetCore.Mvc;
using MSDisTestTask.Data;
using MSDisTestTask.Models;

namespace MSDisTestTask.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IDataStorage _dataStorage;

    public StatsController(IDataStorage dataStorage)
    {
        _dataStorage = dataStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserEventStats>>> GetStats()
    {
        try
        {
            Console.WriteLine("[StatsController] Запрос статистики событий пользователей");
            var stats = await _dataStorage.GetUserEventStatsAsync();
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
    public async Task<ActionResult<IEnumerable<UserEventStats>>> GetUserStats(int userId)
    {
        try
        {
            Console.WriteLine($"[StatsController] Запрос статистики для пользователя: {userId}");
            var allStats = await _dataStorage.GetUserEventStatsAsync();
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
}