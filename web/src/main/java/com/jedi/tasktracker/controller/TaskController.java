package com.jedi.tasktracker.controller;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class TaskController {

    @GetMapping("/")
    public String index(Model model) {
        // Добавляем заглушку данных для отображения
        model.addAttribute("pageTitle", "Трекер задач — Обезьяна и Умник");
        return "tasks";
    }
}