package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.ClassifyResponse;
import com.jedi.tasktracker.client.dto.InboxListResponseDto;
import com.jedi.tasktracker.client.dto.ProjectDto;
import com.jedi.tasktracker.client.dto.RoutineDto;
import com.jedi.tasktracker.client.dto.SprintStatusDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import java.util.List;
import java.util.Map;

public interface ApiClient {

  List<TaskDto> getTasks();

  List<TaskDto> getTodayTasks();

  List<TaskDto> getSprintTasks();

  SprintStatusDto getSprintStatus();

  void completeTask(Long id);

  List<TaskDto> getProjectTasks(Long projectId);

  void createTask(String title, String description, Integer projectId);

  void updateTask(Long id, String title, String content, Integer projectId);

  void selectTask(Long id);

  void deselectTask(Long id);

  void deleteTask(Long id);

  InboxListResponseDto getInboxItems();

  void createInboxItem(String title);

  void updateInboxItem(int id, String title);

  void deleteInboxItem(int id);

  void restoreInboxItem(int id);

  ClassifyResponse classifyInboxItem(
      int id, String entityType, String mode, Map<String, Object> entityData);

  List<ProjectDto> getProjects();

  ProjectDto createProject(String name, String description);

  ProjectDto updateProject(Long id, String name, String description);

  void deleteProject(Long id);

  List<RoutineDto> getRoutines();

  RoutineDto createRoutine(String name, int frequency);

  RoutineDto updateRoutine(int id, String name, int frequency);

  void deleteRoutine(int id);
}
