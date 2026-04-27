package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/tasks")
@RequiredArgsConstructor
public class TaskApiController {

  private final ApiClient apiClient;

  @GetMapping
  public List<TaskDto> getTasks() {
    return apiClient.getTasks();
  }

  @GetMapping("/monkey/all")
  public List<TaskDto> getTodayTasks() {
    return apiClient.getTodayTasks();
  }

  @PostMapping
  public ResponseEntity<Void> createTask(@RequestBody Map<String, Object> body) {
    Object projectIdRaw = body.get("projectId");
    Integer projectId = (projectIdRaw != null) ? (Integer) projectIdRaw : null;
    apiClient.createTask((String) body.get("title"), (String) body.get("content"), projectId);
    return ResponseEntity.ok().build();
  }

  @PutMapping("/{id}")
  public ResponseEntity<Void> updateTask(
      @PathVariable Long id, @RequestBody Map<String, String> body) {
    apiClient.updateTask(id, body.get("title"), body.get("content"));
    return ResponseEntity.ok().build();
  }

  @DeleteMapping("/{id}")
  public ResponseEntity<Void> deleteTask(@PathVariable Long id) {
    apiClient.deleteTask(id);
    return ResponseEntity.noContent().build();
  }
}
