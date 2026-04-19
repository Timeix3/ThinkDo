package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import com.jedi.tasktracker.client.dto.InboxListResponseDto;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/inbox")
@RequiredArgsConstructor
public class InboxApiController {

  private final ApiClient apiClient;

  @GetMapping
  public InboxListResponseDto getInboxItems() {
    return apiClient.getInboxItems();
  }

  @PostMapping
  public ResponseEntity<Void> createInboxItem(@RequestBody Map<String, String> body) {
    apiClient.createInboxItem(body.get("title"));
    return ResponseEntity.status(201).build();
  }

  @DeleteMapping("/{id}")
  public ResponseEntity<Void> deleteInboxItem(@PathVariable int id) {
    apiClient.deleteInboxItem(id);
    return ResponseEntity.noContent().build();
  }

  @PatchMapping("/{id}/restore")
  public ResponseEntity<Void> restoreInboxItem(@PathVariable int id) {
    apiClient.restoreInboxItem(id);
    return ResponseEntity.noContent().build();
  }
}
