package com.jedi.tasktracker.client.dto;

import java.util.List;

public record InboxListResponseDto(List<InboxItemDto> items, boolean inboxOverflow) {}
