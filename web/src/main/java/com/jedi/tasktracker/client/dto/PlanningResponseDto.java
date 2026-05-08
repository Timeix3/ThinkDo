package com.jedi.tasktracker.client.dto;

import java.util.List;

public record PlanningResponseDto(List<PlanningProjectDto> projects, int totalProjects) {}