package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.ProjectDto;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.client.RestClientException;

@RestController
@RequestMapping("/api/projects")
@RequiredArgsConstructor
public class ProjectApiController {
  private final ApiClient apiClient;

  @GetMapping
  public List<ProjectDto> getProjects() {
    return apiClient.getProjects();
  }

  @PostMapping
  public ResponseEntity<ProjectDto> createProject(@RequestBody Map<String, String> body) {
    String name = body.get("name");
    String description = body.get("description");

    var createdProject = apiClient.createProject(name, description);
    return ResponseEntity.status(201).body(createdProject);
  }

  @PutMapping("/{id}")
  public ResponseEntity<ProjectDto> updateProject(
      @PathVariable Long id, @RequestBody Map<String, String> body) {
    String name = body.get("name");
    String description = body.get("description");
    try {
      var updated = apiClient.updateProject(id, name, description);
      return ResponseEntity.ok(updated);
    } catch (RestClientException ex) {
      return ResponseEntity.status(404).build();
    } catch (Exception ex) {
      return ResponseEntity.status(500).build();
    }
  }

  @DeleteMapping("/{id}")
  public ResponseEntity<Void> deleteProject(@PathVariable Long id) {
    apiClient.deleteProject(id);
    return ResponseEntity.noContent().build();
  }
}
