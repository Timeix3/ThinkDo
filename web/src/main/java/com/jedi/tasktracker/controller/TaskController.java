package com.jedi.tasktracker.controller;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class TaskController {

    @GetMapping({"/tasks", "/projects"})
    public String tasks(Model model) {
        model.addAttribute("pageTitle", "Обезьяна и Умник");
        return "tasks";
    }
}
