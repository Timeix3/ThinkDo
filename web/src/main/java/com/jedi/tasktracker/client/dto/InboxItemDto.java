package com.jedi.tasktracker.client.dto;

import java.time.Instant;

public record InboxItemDto(int id, String title, Instant createdAt, Instant updatedAt) {}
