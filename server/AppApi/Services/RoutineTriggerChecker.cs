using Common.Enums;
using Common.Models;

namespace AppApi.Services;

/// <summary>
/// Определяет, должна ли рутина создать запись в инбоксе прямо сейчас.
/// Вынесен в отдельный статический класс для юнит-тестирования без зависимостей.
/// </summary>
public static class RoutineTriggerChecker
{
    /// <summary>
    /// Возвращает true, если для рутины наступило время создать запись в инбоксе.
    /// </summary>
    /// <param name="routine">Рутина.</param>
    /// <param name="utcNow">Текущее UTC-время (инжектируется для тестируемости).</param>
    public static bool ShouldTrigger(Routine routine, DateTime utcNow)
    {
        // Удалённые рутины не обрабатываются
        if (routine.DeletedAt != null)
            return false;

        var today = utcNow.Date;

        return routine.Frequency switch
        {
            RoutineFrequency.Daily => ShouldTriggerDaily(routine.LastTriggeredAt, today),
            RoutineFrequency.Weekly => ShouldTriggerWeekly(routine.CreatedAt, routine.LastTriggeredAt, today),
            RoutineFrequency.Monthly => ShouldTriggerMonthly(routine.CreatedAt, routine.LastTriggeredAt, today),
            _ => false
        };
    }

    /// <summary>
    /// Daily: триггер, если сегодня ещё не было создания.
    /// </summary>
    internal static bool ShouldTriggerDaily(DateTime? lastTriggeredAt, DateTime today)
    {
        if (lastTriggeredAt == null)
            return true;

        return lastTriggeredAt.Value.Date < today;
    }

    /// <summary>
    /// Weekly: триггер в тот же день недели, что и день создания рутины.
    /// Не срабатывает, если уже срабатывал на этой неделе (то есть в последние 7 дней).
    /// </summary>
    internal static bool ShouldTriggerWeekly(DateTime createdAt, DateTime? lastTriggeredAt, DateTime today)
    {
        // Проверяем день недели: триггер только в "свой" день
        if (today.DayOfWeek != createdAt.DayOfWeek)
            return false;

        if (lastTriggeredAt == null)
            return true;

        // Последний триггер должен быть раньше текущей недели (≥ 7 дней назад)
        return lastTriggeredAt.Value.Date < today.AddDays(-6);
    }

    /// <summary>
    /// Monthly: триггер в число месяца, соответствующее дню создания рутины.
    /// Если в текущем месяце такого числа нет (напр. рутина создана 31-го),
    /// используется последний день текущего месяца.
    /// </summary>
    internal static bool ShouldTriggerMonthly(DateTime createdAt, DateTime? lastTriggeredAt, DateTime today)
    {
        // Вычисляем целевой день в текущем месяце
        int daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
        int targetDay = Math.Min(createdAt.Day, daysInCurrentMonth);

        if (today.Day != targetDay)
            return false;

        if (lastTriggeredAt == null)
            return true;

        // Уже срабатывал в этом месяце — пропускаем
        var lastDate = lastTriggeredAt.Value.Date;
        return lastDate.Year < today.Year
            || (lastDate.Year == today.Year && lastDate.Month < today.Month);
    }
}