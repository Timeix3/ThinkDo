package com.jedi.tasktracker.controller;

import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;

@Controller
public class MonkeyController {

  @GetMapping({"/monkey", "/monkey/"})
  public String monkey(Model model) {
    model.addAttribute("pageTitle", "Обезьяна и Умник");
    return "monkey";
  }
}
