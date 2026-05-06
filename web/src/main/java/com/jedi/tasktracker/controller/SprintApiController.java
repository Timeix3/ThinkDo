package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.SprintStatusDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/sprint")
@RequiredArgsConstructor
public class SprintApiController {

  private final ApiClient apiClient;

  @GetMapping("/tasks")
  public List<TaskDto> getSprintTasks() {
    return apiClient.getSprintTasks();
  }

  @GetMapping("/status")
  public SprintStatusDto getSprintStatus() {
    return apiClient.getSprintStatus();
  }
}
