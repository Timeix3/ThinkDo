package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
@RequiredArgsConstructor
public class TaskController {

    private final ApiClient apiClient;

    @GetMapping("/")
    public String index(Model model) {
        model.addAttribute("pageTitle", "Трекер задач — Обезьяна и Умник");
        model.addAttribute("tasks", apiClient.getTasks());
        return "tasks";
    }

    @PostMapping("/tasks")
    public String createTask(
            @RequestParam String title,
            @RequestParam(required = false, defaultValue = "") String description) {
        apiClient.createTask(title, description);
        return "redirect:/";
    }

    @PostMapping("/tasks/{id}/delete")
    public String deleteTask(@PathVariable Long id) {
        apiClient.deleteTask(id);
        return "redirect:/";
    }
}
