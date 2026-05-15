package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/flow")
@RequiredArgsConstructor
public class FlowApiController {
  private final ApiClient apiClient;

  @PutMapping("/phase")
  public ResponseEntity<Void> updatePhase(@RequestBody Map<String, String> body) {
    apiClient.updateFlowPhase(body.get("phase"));
    return ResponseEntity.ok().build();
  }
}
