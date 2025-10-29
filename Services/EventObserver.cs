using MSDisTestTask.Data;
using MSDisTestTask.Models;
using System.Collections.Concurrent;

namespace MSDisTestTask.Services;

public class EventObserver : IObserver<UserEvent>, IDisposable
{
    private readonly IDataStorage _dataStorage;
    private readonly ConcurrentDictionary<(int UserId, string EventType), int> _eventCounts;
    private readonly Timer _saveTimer;
    private bool _disposed = false;

    public EventObserver(IDataStorage dataStorage)
    {
        _dataStorage = dataStorage;
        _eventCounts = new ConcurrentDictionary<(int, string), int>();
        
        // Сохраняем статистику каждые 30 секунд
        _saveTimer = new Timer(SaveStatistics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        Console.WriteLine("[EventObserver] Инициализирован Observer для обработки событий");
    }

    public void OnNext(UserEvent value)
    {
        try
        {
            var key = (value.UserId, value.EventType);
            var newCount = _eventCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
            
            Console.WriteLine($"[EventObserver] Обработано событие: UserId={value.UserId}, EventType={value.EventType}, Текущий счетчик={newCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EventObserver] Ошибка при обработке события: {ex.Message}");
        }
    }

    public void OnError(Exception error)
    {
        Console.WriteLine($"[EventObserver] Получена ошибка в потоке: {error.Message}");
        // Сохраняем текущую статистику перед завершением
        SaveStatistics(null);
    }

    public void OnCompleted()
    {
        Console.WriteLine("[EventObserver] Поток событий завершен");
        // Сохраняем финальную статистику
        SaveStatistics(null);
    }

    private async void SaveStatistics(object? state)
    {
        if (_eventCounts.IsEmpty)
        {
            Console.WriteLine("[EventObserver] Нет данных для сохранения");
            return;
        }

        try
        {
            var stats = _eventCounts.Select(kvp => new UserEventStats
            {
                UserId = kvp.Key.UserId,
                EventType = kvp.Key.EventType,
                Count = kvp.Value
            }).ToList();

            Console.WriteLine($"[EventObserver] Сохранение статистики: {stats.Count} записей");
            
            await _dataStorage.SaveUserEventStatsAsync(stats);
            
            Console.WriteLine("[EventObserver] Статистика успешно сохранена");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EventObserver] Ошибка при сохранении статистики: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Console.WriteLine("[EventObserver] Освобождение ресурсов EventObserver");
            _saveTimer?.Dispose();
            SaveStatistics(null);
            _disposed = true;
        }
    }
}