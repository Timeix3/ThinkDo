package com.jedi.tasktracker.client.dto;

public record ProjectDto(Long id, String name, String description, boolean isDefault) {}