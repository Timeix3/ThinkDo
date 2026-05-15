package com.jedi.tasktracker.controller;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class TaskController {

  @GetMapping({"/tasks", "/planning", "/inbox", "/projects", "/routines"})
  public String tasks(Model model, HttpServletRequest request) {
    model.addAttribute("pageTitle", "Обезьяна и Умник");

    // Передаем текущий путь, чтобы фронтенд знал, какую вкладку открыть первой
    model.addAttribute("initialPath", request.getRequestURI());

    return "tasks";
  }
}
