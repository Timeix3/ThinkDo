package com.jedi.tasktracker.client.dto;

public record SprintStatusDto(
    Boolean hasActiveSprint, int pendingTasksCount, int inboxCount, String phase) {}