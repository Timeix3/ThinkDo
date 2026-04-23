using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Enums;

namespace AppApi.Services;

/// <summary>
/// Фоновый сервис (Hosted Service), который периодически проверяет рутины
/// и создаёт записи в инбоксе пользователей по расписанию.
///
/// Стратегия при пропущенных запусках: создаётся запись только за текущий день,
/// пропущенные дни не компенсируются.
/// </summary>
public class RoutineSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoutineSchedulerService> _logger;

    /// <summary>
    /// Интервал между проверками. По умолчанию — 15 минут.
    /// Настраивается через appsettings: "RoutineScheduler:CheckIntervalMinutes"
    /// </summary>
    private readonly TimeSpan _checkInterval;

    public RoutineSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<RoutineSchedulerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var intervalMinutes = configuration.GetValue<int>("RoutineScheduler:CheckIntervalMinutes", 15);
        _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RoutineSchedulerService started. Check interval: {Interval}", _checkInterval);

        // Небольшая задержка при старте — даём приложению полностью инициализироваться
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessRoutinesAsync(stoppingToken);

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Приложение остановлено — выходим из цикла
                break;
            }
        }

        _logger.LogInformation("RoutineSchedulerService stopped.");
    }

    private async Task ProcessRoutinesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("RoutineSchedulerService: starting routine check at {Time}", DateTime.UtcNow);

        // Используем scoped DI (BackgroundService — singleton, репозитории — scoped)
        using var scope = _serviceProvider.CreateScope();
        var routineRepo = scope.ServiceProvider.GetRequiredService<IRoutineRepository>();
        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();

        IEnumerable<Common.Models.Routine> routines;

        try
        {
            routines = await routineRepo.GetAllActiveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RoutineSchedulerService: failed to load routines");
            return;
        }

        var utcNow = DateTime.UtcNow;
        int triggered = 0;
        int skipped = 0;

        foreach (var routine in routines)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!RoutineTriggerChecker.ShouldTrigger(routine, utcNow))
            {
                skipped++;
                continue;
            }

            try
            {
                await inboxService.CreateItemAsync(
                    new CreateInboxItemDto { Title = routine.Name },
                    routine.UserId);

                await routineRepo.UpdateLastTriggeredAtAsync(routine.Id, utcNow);

                triggered++;

                _logger.LogInformation(
                    "RoutineSchedulerService: created inbox item for routine {RoutineId} " +
                    "('{Name}', {Frequency}) for user {UserId}",
                    routine.Id, routine.Name, routine.Frequency, routine.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RoutineSchedulerService: failed to process routine {RoutineId} for user {UserId}",
                    routine.Id, routine.UserId);
            }
        }

        _logger.LogDebug(
            "RoutineSchedulerService: check complete. Triggered: {Triggered}, Skipped: {Skipped}",
            triggered, skipped);
    }
}