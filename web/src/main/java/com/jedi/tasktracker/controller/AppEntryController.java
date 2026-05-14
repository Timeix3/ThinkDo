package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
@RequiredArgsConstructor
public class AppEntryController {

  private final ApiClient apiClient;

  @GetMapping("/")
  public String index() {
    String phase;
    try {
      phase = apiClient.getCurrentPhase();
    } catch (Exception e) {
      phase = "sprint";
    }
    switch (phase) {
      case "sprint" -> {
        return "redirect:/monkey";
      }
      case "review" -> {
        return "redirect:/projects?section=inbox";
      }
      case "planning" -> {
        return "redirect:/projects?section=dashboard";
      }
      default -> {
        return "redirect:/monkey";
      }
    }
  }
}
