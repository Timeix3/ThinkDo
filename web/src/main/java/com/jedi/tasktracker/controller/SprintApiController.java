package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.SprintStatusDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.client.RestClientResponseException;

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

  @PostMapping("/start")
  public ResponseEntity<?> startSprint(@RequestBody StartSprintRequest request) {
    try {
      apiClient.startSprint(request.taskIds());
      return ResponseEntity.ok().build();
    } catch (RestClientResponseException ex) {
      String message = ex.getResponseBodyAsString();
      if (message == null || message.isBlank()) {
        message = ex.getMessage();
      }
      return ResponseEntity.status(ex.getStatusCode()).body(Map.of("message", message));
    }
  }

  @PostMapping("/complete")
  public ResponseEntity<Void> completeSprint() {
    apiClient.completeSprint();
    return ResponseEntity.ok().build();
  }

  public record StartSprintRequest(List<Long> taskIds) {}
}
