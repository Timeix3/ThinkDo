package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.InboxListResponseDto;
import com.jedi.tasktracker.client.dto.ProjectDto;
import com.jedi.tasktracker.client.dto.TaskDto;
import com.jedi.tasktracker.client.dto.TaskListResponseDto;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import lombok.RequiredArgsConstructor;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.MediaType;
import org.springframework.web.client.RestClient;

@RequiredArgsConstructor
public class DefaultApiClient implements ApiClient {

  private final RestClient restClient;

  @Override
  public List<TaskDto> getTasks() {
    var resp =
        restClient
            .get()
            .uri("/api/tasks")
            .retrieve()
            .body(new ParameterizedTypeReference<TaskListResponseDto>() {});
    return resp.items();
  }

  @Override
  public List<TaskDto> getTodayTasks() {
    return restClient
        .get()
        .uri("/api/tasks/monkey/all")
        .retrieve()
        .body(new ParameterizedTypeReference<List<TaskDto>>() {});
  }

  @Override
  public List<TaskDto> getProjectTasks(Long projectId) {
    return restClient
        .get()
        .uri("/api/projects/{id}/tasks", projectId)
        .retrieve()
        .body(new ParameterizedTypeReference<List<TaskDto>>() {});
  }

  @Override
  public void createTask(String title, String description, Integer projectId) {
    Map<String, Object> requestBody = new HashMap<>();
    requestBody.put("title", title);
    requestBody.put("content", description != null ? description : "");
    requestBody.put("projectId", projectId);

    restClient
        .post()
        .uri("/api/tasks")
        .contentType(MediaType.APPLICATION_JSON)
        .body(requestBody)
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void updateTask(Long id, String title, String content) {
    restClient
        .put()
        .uri("/api/tasks/{id}", id)
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title, "content", content != null ? content : ""))
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void deleteTask(Long id) {
    restClient.delete().uri("/api/tasks/{id}", id).retrieve().toBodilessEntity();
  }

  @Override
  public InboxListResponseDto getInboxItems() {
    return restClient.get().uri("/api/inbox").retrieve().body(InboxListResponseDto.class);
  }

  @Override
  public void createInboxItem(String title) {
    restClient
        .post()
        .uri("/api/inbox")
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title))
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void updateInboxItem(int id, String title) {
    restClient
        .put()
        .uri("/api/inbox/{id}", id)
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title))
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void deleteInboxItem(int id) {
    restClient.delete().uri("/api/inbox/{id}", id).retrieve().toBodilessEntity();
  }

  @Override
  public void restoreInboxItem(int id) {
    restClient
        .patch()
        .uri("/api/inbox/{id}/restore", id)
        .contentType(MediaType.APPLICATION_JSON)
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public List<ProjectDto> getProjects() {
    return restClient
        .get()
        .uri("/api/projects")
        .retrieve()
        .body(new ParameterizedTypeReference<List<ProjectDto>>() {});
  }

  @Override
  public ProjectDto createProject(String name, String description) {
    return restClient
        .post()
        .uri("/api/projects")
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("name", name, "description", description != null ? description : ""))
        .retrieve()
        .body(ProjectDto.class);
  }

  @Override
  public ProjectDto updateProject(Long id, String name, String description) {
    return restClient
        .put()
        .uri("/api/projects/{id}", id)
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("name", name, "description", description != null ? description : ""))
        .retrieve()
        .body(ProjectDto.class);
  }

  @Override
  public void deleteProject(Long id) {
    restClient.delete().uri("/api/projects/{id}", id).retrieve().toBodilessEntity();
  }
}
