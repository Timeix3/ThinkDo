package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.SprintStatusDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.http.ResponseEntity;

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

@PostMapping("/complete")
  public ResponseEntity<Void> completeSprint() {
  apiClient.completeSprint();
  return ResponseEntity.ok().build();
  }
}