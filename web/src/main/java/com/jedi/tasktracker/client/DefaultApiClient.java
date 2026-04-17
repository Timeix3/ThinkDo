package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.TaskDto;
import com.jedi.tasktracker.client.dto.TaskListResponseDto;
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
  public void createTask(String title, String description) {
    restClient
        .post()
        .uri("/api/tasks")
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title, "content", description != null ? description : ""))
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
}
