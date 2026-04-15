package com.jedi.tasktracker.model;

import java.time.LocalDateTime;
import java.util.UUID;

public class TaskDto {
    private UUID id;
    private String title;
    private String status;       // available, blocked, completed, cancelled
    private String projectName;  // название проекта (если привязана)
    private LocalDateTime createdAt;

    // Конструктор по умолчанию (нужен для JSON/Thymeleaf)
    public TaskDto() {}

    // Геттеры и сеттеры (можно сгенерировать в VS Code: клик правой кнопкой → Source Action → Generate Getters and Setters)
    public UUID getId() { return id; }
    public void setId(UUID id) { this.id = id; }

    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }

    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }

    public String getProjectName() { return projectName; }
    public void setProjectName(String projectName) { this.projectName = projectName; }

    public LocalDateTime getCreatedAt() { return createdAt; }
    public void setCreatedAt(LocalDateTime createdAt) { this.createdAt = createdAt; }
}