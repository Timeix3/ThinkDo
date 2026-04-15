package com.jedi.tasktracker.service;

import com.jedi.tasktracker.model.TaskDto;
import org.springframework.stereotype.Service;
import java.time.LocalDateTime;
import java.util.*;

@Service
public class TaskService {

    // Пока мок-данные. Потом заменим на реальный вызов бэкенда.
    public List<TaskDto> getUserTasks() {
        TaskDto task1 = new TaskDto();
        task1.setId(UUID.randomUUID());
        task1.setTitle("Сделать страницу списка задач");
        task1.setStatus("available");
        task1.setProjectName("ThinkDo");
        task1.setCreatedAt(LocalDateTime.now().minusDays(1));

        TaskDto task2 = new TaskDto();
        task2.setId(UUID.randomUUID());
        task2.setTitle("Обсудить API с бэкендом");
        task2.setStatus("blocked");
        task2.setProjectName(null);
        task2.setCreatedAt(LocalDateTime.now().minusHours(5));

        return Arrays.asList(task1, task2);
    }
}