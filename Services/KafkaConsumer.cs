using Confluent.Kafka;
using MSDisTestTask.Models;
using System.Text.Json;

namespace MSDisTestTask.Services;

public class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly EventObservable _eventObservable;
    private readonly string _topic;

    public KafkaConsumer(IConfiguration configuration, EventObservable eventObservable)
    {
        _eventObservable = eventObservable;
        _topic = configuration["KAFKA_TOPIC"] ?? "user-events";

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092",
            GroupId = configuration["KAFKA_GROUP_ID"] ?? "user-events-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            StatisticsIntervalMs = 5000,
            SessionTimeoutMs = 6000,
            AutoCommitIntervalMs = 5000,
            EnablePartitionEof = true
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, e) => Console.WriteLine($"[KafkaConsumer] Ошибка Kafka: {e.Reason}"))
            .SetStatisticsHandler((_, json) => Console.WriteLine($"[KafkaConsumer] Статистика Kafka: {json}"))
            .SetPartitionsAssignedHandler((c, partitions) =>
            {
                Console.WriteLine($"[KafkaConsumer] Назначены партиции: {string.Join(", ", partitions)}");
            })
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                Console.WriteLine($"[KafkaConsumer] Отозваны партиции: {string.Join(", ", partitions)}");
            })
            .Build();

        Console.WriteLine($"[KafkaConsumer] Инициализирован Kafka Consumer для топика: {_topic}");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            Console.WriteLine("[KafkaConsumer] Запуск Kafka Consumer");
            
            try
            {
                _consumer.Subscribe(_topic);
                Console.WriteLine($"[KafkaConsumer] Подписка на топик: {_topic}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);
                        
                        if (consumeResult.IsPartitionEOF)
                        {
                            Console.WriteLine($"[KafkaConsumer] Достигнут конец партиции {consumeResult.TopicPartition}");
                            continue;
                        }

                        if (consumeResult?.Message?.Value != null)
                        {
                            Console.WriteLine($"[KafkaConsumer] Получено сообщение: Offset={consumeResult.Offset}, Partition={consumeResult.Partition}");
                            
                            var userEvent = JsonSerializer.Deserialize<UserEvent>(consumeResult.Message.Value);
                            
                            if (userEvent != null)
                            {
                                Console.WriteLine($"[KafkaConsumer] Десериализовано событие: UserId={userEvent.UserId}, EventType={userEvent.EventType}");
                                _eventObservable.PublishEvent(userEvent);
                            }
                            else
                            {
                                Console.WriteLine("[KafkaConsumer] Не удалось десериализовать событие");
                            }
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"[KafkaConsumer] Ошибка при потреблении сообщения: {ex.Error.Reason}");
                        
                        if (ex.Error.IsFatal)
                        {
                            Console.WriteLine("[KafkaConsumer] Критическая ошибка Kafka, завершение работы");
                            break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[KafkaConsumer] Ошибка десериализации JSON: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[KafkaConsumer] Неожиданная ошибка: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[KafkaConsumer] Операция отменена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KafkaConsumer] Критическая ошибка: {ex.Message}");
                _eventObservable.Error(ex);
            }
            finally
            {
                Console.WriteLine("[KafkaConsumer] Закрытие Kafka Consumer");
                _consumer.Close();
                _eventObservable.Complete();
            }
        }, stoppingToken);
    }

    public override void Dispose()
    {
        Console.WriteLine("[KafkaConsumer] Освобождение ресурсов KafkaConsumer");
        _consumer?.Dispose();
        base.Dispose();
    }
}