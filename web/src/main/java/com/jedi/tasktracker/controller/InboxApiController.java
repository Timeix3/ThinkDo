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

  @PutMapping("/{id}")
  public ResponseEntity<Void> updateInboxItem(
      @PathVariable int id, @RequestBody Map<String, String> body) {
    apiClient.updateInboxItem(id, body.get("title"));
    return ResponseEntity.noContent().build();
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

  @PostMapping("/{id}/classify")
  public ResponseEntity<Void> classifyInboxItem(
      @PathVariable int id, @RequestBody Map<String, Object> classifyRequest) {
    String targetType = (String) classifyRequest.get("targetType");
    Map<String, Object> data = (Map<String, Object>) classifyRequest.get("data");
    apiClient.classifyInboxItem(id, targetType, data);
    return ResponseEntity.ok().build();
  }
}
