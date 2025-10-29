using MSDisTestTask.Models;
using System.Text.Json;

namespace MSDisTestTask.Data;

public class FileDataStorage : IDataStorage
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock;

    public FileDataStorage(IConfiguration configuration)
    {
        _filePath = "user_event_stats.json";
        _fileLock = new SemaphoreSlim(1, 1);
        
        Console.WriteLine($"[FileDataStorage] Инициализирован файловый storage: {_filePath}");
    }

    public async Task SaveUserEventStatsAsync(IEnumerable<UserEventStats> stats)
    {
        if (!stats.Any())
        {
            Console.WriteLine("[FileDataStorage] Нет данных для сохранения");
            return;
        }

        await _fileLock.WaitAsync();
        try
        {
            Console.WriteLine($"[FileDataStorage] Сохранение {stats.Count()} записей в файл: {_filePath}");

            var existingStats = new Dictionary<(int UserId, string EventType), UserEventStats>();
            
            if (File.Exists(_filePath))
            {
                var existingJson = await File.ReadAllTextAsync(_filePath);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    var existing = JsonSerializer.Deserialize<List<UserEventStats>>(existingJson) ?? new List<UserEventStats>();
                    foreach (var stat in existing)
                    {
                        existingStats[(stat.UserId, stat.EventType)] = stat;
                    }
                }
            }

            // Обновляем статистику
            foreach (var newStat in stats)
            {
                var key = (newStat.UserId, newStat.EventType);
                if (existingStats.ContainsKey(key))
                {
                    existingStats[key].Count += newStat.Count;
                }
                else
                {
                    existingStats[key] = newStat;
                }
            }

            // Сохраняем обновленные данные
            var allStats = existingStats.Values.OrderBy(s => s.UserId).ThenBy(s => s.EventType).ToList();
            var json = JsonSerializer.Serialize(allStats, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(_filePath, json);
            Console.WriteLine($"[FileDataStorage] Статистика успешно сохранена в файл: {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDataStorage] Ошибка сохранения в файл: {ex.Message}");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<UserEventStats>> GetUserEventStatsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            Console.WriteLine($"[FileDataStorage] Чтение статистики из файла: {_filePath}");

            if (!File.Exists(_filePath))
            {
                Console.WriteLine("[FileDataStorage] Файл статистики не существует, возвращаем пустой список");
                return new List<UserEventStats>();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[FileDataStorage] Файл статистики пуст, возвращаем пустой список");
                return new List<UserEventStats>();
            }

            var stats = JsonSerializer.Deserialize<List<UserEventStats>>(json) ?? new List<UserEventStats>();
            Console.WriteLine($"[FileDataStorage] Прочитано {stats.Count} записей статистики");
            
            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileDataStorage] Ошибка чтения файла: {ex.Message}");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }
}