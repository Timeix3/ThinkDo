package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.PlanningProjectDto;
import com.jedi.tasktracker.client.dto.PlanningResponseDto;
import com.jedi.tasktracker.client.dto.ProjectDto;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/planning")
@RequiredArgsConstructor
public class PlanningApiController {

  private final ApiClient apiClient;

  @GetMapping("/projects")
  public PlanningResponseDto getProjects() {
    List<ProjectDto> projects = apiClient.getProjects();

    List<PlanningProjectDto> planningProjects =
        projects.stream()
            .map(
                p ->
                    new PlanningProjectDto(
                        p.id(), p.name(), p.description(), apiClient.getProjectTasks(p.id())))
            .toList();

    return new PlanningResponseDto(planningProjects, planningProjects.size());
  }
}
