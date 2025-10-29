using MSDisTestTask.Models;

namespace MSDisTestTask.Data;

public interface IDataStorage
{
    Task SaveUserEventStatsAsync(IEnumerable<UserEventStats> stats);
    Task<IEnumerable<UserEventStats>> GetUserEventStatsAsync();
}