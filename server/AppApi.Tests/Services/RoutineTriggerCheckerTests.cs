using AppApi.Services;
using Common.Enums;
using Common.Models;
using FluentAssertions;

namespace AppApi.Tests.Services;

/// <summary>
/// Тесты логики определения момента триггера для рутин.
/// Все тесты используют фиксированные даты — никакой зависимости от DateTime.Now.
/// </summary>
public class RoutineTriggerCheckerTests
{
    // ────────────────────────────────────────────────────────────────────────────
    // Вспомогательный метод — строит рутину с минимальными полями
    // ────────────────────────────────────────────────────────────────────────────
    private static Routine MakeRoutine(
        RoutineFrequency frequency,
        DateTime? createdAt = null,
        DateTime? lastTriggeredAt = null,
        DateTime? deletedAt = null)
    {
        return new Routine
        {
            Id = 1,
            UserId = "user-1",
            Name = "Test Routine",
            Frequency = frequency,
            // По умолчанию создана в понедельник, 1-го числа (для удобства Weekly/Monthly тестов)
            CreatedAt = createdAt ?? new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), // понедельник
            LastTriggeredAt = lastTriggeredAt,
            DeletedAt = deletedAt
        };
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ShouldTrigger — общая точка входа
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShouldTrigger_DeletedRoutine_ReturnsFalse()
    {
        var routine = MakeRoutine(RoutineFrequency.Daily, deletedAt: DateTime.UtcNow);
        var utcNow = new DateTime(2026, 4, 23, 10, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTrigger(routine, utcNow).Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Daily
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShouldTriggerDaily_NeverTriggered_ReturnsTrue()
    {
        var today = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerDaily(null, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerDaily_TriggeredYesterday_ReturnsTrue()
    {
        var today = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc);
        var yesterday = today.AddDays(-1);

        RoutineTriggerChecker.ShouldTriggerDaily(yesterday, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerDaily_TriggeredToday_ReturnsFalse()
    {
        var today = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc);
        // Триггер был сегодня в 03:00 UTC
        var triggeredToday = new DateTime(2026, 4, 23, 3, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerDaily(triggeredToday, today).Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerDaily_TriggeredWeekAgo_ReturnsTrue()
    {
        var today = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc);
        var weekAgo = today.AddDays(-7);

        RoutineTriggerChecker.ShouldTriggerDaily(weekAgo, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTrigger_Daily_Integration_TriggeredEarlierToday_ReturnsFalse()
    {
        var routine = MakeRoutine(
            RoutineFrequency.Daily,
            lastTriggeredAt: new DateTime(2026, 4, 23, 1, 0, 0, DateTimeKind.Utc));

        var utcNow = new DateTime(2026, 4, 23, 14, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTrigger(routine, utcNow).Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Weekly
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShouldTriggerWeekly_NeverTriggered_CorrectDayOfWeek_ReturnsTrue()
    {
        // Рутина создана в понедельник; проверяем в понедельник
        var createdAt = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc); // понедельник
        var today = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);    // тоже понедельник
        today.DayOfWeek.Should().Be(DayOfWeek.Monday);

        RoutineTriggerChecker.ShouldTriggerWeekly(createdAt, null, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerWeekly_NeverTriggered_WrongDayOfWeek_ReturnsFalse()
    {
        // Рутина создана в понедельник; проверяем во вторник
        var createdAt = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);  // понедельник
        var today = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);     // вторник
        today.DayOfWeek.Should().Be(DayOfWeek.Tuesday);

        RoutineTriggerChecker.ShouldTriggerWeekly(createdAt, null, today).Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerWeekly_TriggeredLastWeek_CorrectDay_ReturnsTrue()
    {
        var createdAt = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);   // понедельник
        var lastWeekMonday = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc); // пн прошлой нед.
        var thisMonday = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerWeekly(createdAt, lastWeekMonday, thisMonday).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerWeekly_TriggeredThisWeek_ReturnsFalse()
    {
        var createdAt = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        // Последний триггер — вчера (но нам не важно, был ли это "правильный" день — просто < 7 дней назад)
        var recentTrigger = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc); // этот понедельник
        var today = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerWeekly(createdAt, recentTrigger, today).Should().BeFalse();
    }

    [Fact]
    public void ShouldTrigger_Weekly_Integration_WrongDay_ReturnsFalse()
    {
        // Рутина создана в пятницу
        var routine = MakeRoutine(
            RoutineFrequency.Weekly,
            createdAt: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)); // пятница

        // Проверяем в среду
        var utcNow = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc); // среда
        utcNow.DayOfWeek.Should().Be(DayOfWeek.Wednesday);

        RoutineTriggerChecker.ShouldTrigger(routine, utcNow).Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Monthly
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ShouldTriggerMonthly_NeverTriggered_CorrectDay_ReturnsTrue()
    {
        // Рутина создана 15-го — проверяем 15-го
        var createdAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerMonthly_NeverTriggered_WrongDay_ReturnsFalse()
    {
        var createdAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc); // 16-е, не 15-е

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerMonthly_TriggeredLastMonth_CorrectDay_ReturnsTrue()
    {
        var createdAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, lastMonth, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerMonthly_TriggeredThisMonth_ReturnsFalse()
    {
        var createdAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var thisMonth = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, thisMonth, today).Should().BeFalse();
    }

    // ── Краевой случай: рутина создана 31-го ─────────────────────────────────

    [Fact]
    public void ShouldTriggerMonthly_CreatedOn31st_FebruaryUsesLastDay_28thReturnsTrue()
    {
        // Рутина создана 31 января — в феврале (28 дней в 2026) целевой день = 28
        var createdAt = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerMonthly_CreatedOn31st_FebruaryNon28thDay_ReturnsFalse()
    {
        var createdAt = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 2, 27, 0, 0, 0, DateTimeKind.Utc); // не 28-е

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerMonthly_CreatedOn31st_AprilUsesLastDay_30thReturnsTrue()
    {
        // Апрель — 30 дней; целевой день = 30
        var createdAt = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerMonthly_CreatedOn31st_JulyHas31Days_ReturnsTrue()
    {
        var createdAt = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

        RoutineTriggerChecker.ShouldTriggerMonthly(createdAt, null, today).Should().BeTrue();
    }

    // ── Краевой случай: сервер был выключен несколько дней ───────────────────

    [Fact]
    public void ShouldTrigger_Daily_ServerDownSeveralDays_OnlyTriggersOnce()
    {
        // Сервер не запускался 5 дней. При запуске создаётся запись только за сегодня.
        // Каждый отдельный вызов ProcessRoutines — это один момент времени.
        var lastTrigger = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc); // 5 дней назад
        var today = new DateTime(2026, 4, 23, 0, 0, 0, DateTimeKind.Utc);

        // Первый вызов — триггер должен сработать
        RoutineTriggerChecker.ShouldTriggerDaily(lastTrigger, today).Should().BeTrue();

        // После записи last_triggered_at = utcNow; следующий вызов того же дня — не срабатывает
        RoutineTriggerChecker.ShouldTriggerDaily(today, today).Should().BeFalse();
    }

    // ── Краевой случай: нет рутин ────────────────────────────────────────────

    [Fact]
    public void ShouldTrigger_EmptyRoutineList_ProducesNoTriggers()
    {
        var routines = Array.Empty<Routine>();
        var utcNow = DateTime.UtcNow;

        var triggeredCount = routines.Count(r => RoutineTriggerChecker.ShouldTrigger(r, utcNow));

        triggeredCount.Should().Be(0);
    }
}