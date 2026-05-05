package com.jedi.tasktracker.client.dto;

public record TaskDto(
    Long id,
    String title,
    String content,
    Integer projectId,
    String projectName,
    Boolean isSelectedForSprint,
    Integer status) {}
