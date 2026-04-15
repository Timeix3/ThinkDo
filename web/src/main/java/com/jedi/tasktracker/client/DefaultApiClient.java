package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.TaskDto;
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
    return restClient
        .get()
        .uri("/api/admin/tasks")
        .retrieve()
        .body(new ParameterizedTypeReference<>() {});
  }

  @Override
  public void createTask(String title, String description) {
    restClient
        .post()
        .uri("/api/admin/tasks")
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title, "content", description != null ? description : ""))
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void updateTask(Long id, String title, String content) {
    restClient
        .put()
        .uri("/api/admin/tasks/{id}", id)
        .contentType(MediaType.APPLICATION_JSON)
        .body(Map.of("title", title, "content", content != null ? content : ""))
        .retrieve()
        .toBodilessEntity();
  }

  @Override
  public void deleteTask(Long id) {
    restClient.delete().uri("/api/admin/tasks/{id}", id).retrieve().toBodilessEntity();
  }
}
