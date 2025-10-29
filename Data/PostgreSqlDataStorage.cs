using MSDisTestTask.Models;
using Npgsql;
using System.Text;

namespace MSDisTestTask.Data;

public class PostgreSqlDataStorage : IDataStorage
{
    private readonly string _connectionString;

    public PostgreSqlDataStorage(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["POSTGRES_CONNECTION_STRING"] 
            ?? throw new InvalidOperationException("PostgreSQL connection string не найдена");
        
        Console.WriteLine("[PostgreSqlDataStorage] Инициализирован PostgreSQL storage");
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            Console.WriteLine("[PostgreSqlDataStorage] Инициализация базы данных");
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS user_event_stats (
                    user_id INT NOT NULL,
                    event_type VARCHAR(50) NOT NULL,
                    count INT NOT NULL,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (user_id, event_type)
                );
                
                CREATE INDEX IF NOT EXISTS idx_user_event_stats_user_id ON user_event_stats(user_id);
                CREATE INDEX IF NOT EXISTS idx_user_event_stats_event_type ON user_event_stats(event_type);
            ";

            using var command = new NpgsqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine("[PostgreSqlDataStorage] Таблица user_event_stats создана/проверена");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgreSqlDataStorage] Ошибка инициализации БД: {ex.Message}");
            throw;
        }
    }

    public async Task SaveUserEventStatsAsync(IEnumerable<UserEventStats> stats)
    {
        if (!stats.Any())
        {
            Console.WriteLine("[PostgreSqlDataStorage] Нет данных для сохранения");
            return;
        }

        try
        {
            Console.WriteLine($"[PostgreSqlDataStorage] Сохранение {stats.Count()} записей статистики");
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            var sql = @"
                INSERT INTO user_event_stats (user_id, event_type, count, updated_at) 
                VALUES (@user_id, @event_type, @count, CURRENT_TIMESTAMP)
                ON CONFLICT (user_id, event_type) 
                DO UPDATE SET 
                    count = user_event_stats.count + EXCLUDED.count,
                    updated_at = CURRENT_TIMESTAMP
            ";

            foreach (var stat in stats)
            {
                using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@user_id", stat.UserId);
                command.Parameters.AddWithValue("@event_type", stat.EventType);
                command.Parameters.AddWithValue("@count", stat.Count);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            Console.WriteLine("[PostgreSqlDataStorage] Статистика успешно сохранена в PostgreSQL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgreSqlDataStorage] Ошибка сохранения в PostgreSQL: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<UserEventStats>> GetUserEventStatsAsync()
    {
        try
        {
            Console.WriteLine("[PostgreSqlDataStorage] Получение статистики из PostgreSQL");
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT user_id, event_type, count FROM user_event_stats ORDER BY user_id, event_type";
            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var results = new List<UserEventStats>();
            while (await reader.ReadAsync())
            {
                results.Add(new UserEventStats
                {
                    UserId = reader.GetInt32(0),
                    EventType = reader.GetString(1),
                    Count = reader.GetInt32(2)
                });
            }

            Console.WriteLine($"[PostgreSqlDataStorage] Получено {results.Count} записей статистики");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostgreSqlDataStorage] Ошибка получения данных из PostgreSQL: {ex.Message}");
            throw;
        }
    }
}