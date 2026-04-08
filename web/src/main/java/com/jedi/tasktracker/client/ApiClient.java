package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.TaskDto;

import java.util.List;

public interface ApiClient {

    List<TaskDto> getTasks();

    void createTask(String title, String description);

    void deleteTask(Long id);

    void completeTask(Long id);
}
