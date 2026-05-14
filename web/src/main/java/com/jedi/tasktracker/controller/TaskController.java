package com.jedi.tasktracker.controller;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
public class TaskController {

  @GetMapping("/projects")
  public String tasks(Model model, @RequestParam(required = false) String section) {
    model.addAttribute("pageTitle", "Обезьяна и Умник");
    model.addAttribute("initialSection", section);
    return "tasks";
  }
}
