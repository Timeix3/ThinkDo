package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.ProjectDto;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

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
}
