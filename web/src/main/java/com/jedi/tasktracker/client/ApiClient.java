package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.InboxListResponseDto;
import com.jedi.tasktracker.client.dto.ProjectDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;

public interface ApiClient {

  List<TaskDto> getTasks();

  List<TaskDto> getTodayTasks();

  void createTask(String title, String description, Integer projectId);

  void updateTask(Long id, String title, String content);

  void deleteTask(Long id);

  InboxListResponseDto getInboxItems();

  void createInboxItem(String title);

  void updateInboxItem(int id, String title);

  void deleteInboxItem(int id);

  void restoreInboxItem(int id);

  List<ProjectDto> getProjects();

  ProjectDto createProject(String name, String description);

  ProjectDto updateProject(Long id, String name, String description);

  void deleteProject(Long id);
}
