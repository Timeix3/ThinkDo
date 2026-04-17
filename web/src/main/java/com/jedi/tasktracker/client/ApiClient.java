package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;

public interface ApiClient {

  List<TaskDto> getTasks();

  List<TaskDto> getTodayTasks();

  void createTask(String title, String description);

  void updateTask(Long id, String title, String content);

  void deleteTask(Long id);
}
