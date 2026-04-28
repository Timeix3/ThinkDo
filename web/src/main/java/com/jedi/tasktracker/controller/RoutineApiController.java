package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.RoutineDto;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/routines")
@RequiredArgsConstructor
public class RoutineApiController {

  private final ApiClient apiClient;

  @GetMapping
  public List<RoutineDto> getRoutines() {
    return apiClient.getRoutines();
  }

  @PostMapping
  public ResponseEntity<RoutineDto> createRoutine(@RequestBody Map<String, Object> body) {
    String name = (String) body.get("name");
    int frequency = ((Number) body.get("frequency")).intValue();
    RoutineDto created = apiClient.createRoutine(name, frequency);
    return ResponseEntity.ok(created);
  }

  @PutMapping("/{id}")
  public ResponseEntity<RoutineDto> updateRoutine(
      @PathVariable int id, @RequestBody Map<String, Object> body) {
    String name = (String) body.get("name");
    int frequency = ((Number) body.get("frequency")).intValue();
    RoutineDto updated = apiClient.updateRoutine(id, name, frequency);
    return ResponseEntity.ok(updated);
  }

  @DeleteMapping("/{id}")
  public ResponseEntity<Void> deleteRoutine(@PathVariable int id) {
    apiClient.deleteRoutine(id);
    return ResponseEntity.noContent().build();
  }
}
