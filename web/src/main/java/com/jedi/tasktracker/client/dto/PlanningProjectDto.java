package com.jedi.tasktracker.client.dto;

import java.util.List;

public record PlanningProjectDto(Long id, String name, String description, List<TaskDto> tasks) {}
