using MSDisTestTask.Models;
using System.Reactive.Subjects;

namespace MSDisTestTask.Services;

public class EventObservable : IObservable<UserEvent>, IDisposable
{
    private readonly Subject<UserEvent> _subject;
    private bool _disposed = false;

    public EventObservable()
    {
        _subject = new Subject<UserEvent>();
        Console.WriteLine("[EventObservable] Инициализирован Observable для событий пользователей");
    }

    public IDisposable Subscribe(IObserver<UserEvent> observer)
    {
        Console.WriteLine("[EventObservable] Новый подписчик зарегистрирован");
        return _subject.Subscribe(observer);
    }

    public void PublishEvent(UserEvent userEvent)
    {
        if (!_disposed)
        {
            Console.WriteLine($"[EventObservable] Публикация события: UserId={userEvent.UserId}, EventType={userEvent.EventType}, Timestamp={userEvent.Timestamp}");
            _subject.OnNext(userEvent);
        }
    }

    public void Complete()
    {
        if (!_disposed)
        {
            Console.WriteLine("[EventObservable] Завершение потока событий");
            _subject.OnCompleted();
        }
    }

    public void Error(Exception error)
    {
        if (!_disposed)
        {
            Console.WriteLine($"[EventObservable] Ошибка в потоке событий: {error.Message}");
            _subject.OnError(error);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Console.WriteLine("[EventObservable] Освобождение ресурсов EventObservable");
            _subject?.Dispose();
            _disposed = true;
        }
    }
}