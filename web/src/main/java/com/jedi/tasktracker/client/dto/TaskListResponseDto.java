package com.jedi.tasktracker.client.dto;

import java.util.List;

public record TaskListResponseDto(
    List<TaskDto> items,
    int totalCount,
    int pageSize,
    int pageNumber,
    boolean hasMore
) {}