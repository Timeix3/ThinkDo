package com.jedi.tasktracker.controller;

import com.jedi.tasktracker.client.ApiClient;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
@RequiredArgsConstructor
public class TaskController {

    private final ApiClient apiClient;

    @GetMapping("/")
    public String index(Model model) {
        model.addAttribute("pageTitle", "Трекер задач — Обезьяна и Умник");
        return "tasks";
    }
}