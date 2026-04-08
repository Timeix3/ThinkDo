package com.jedi.tasktracker.client;

import com.jedi.tasktracker.client.dto.TaskDto;
import lombok.RequiredArgsConstructor;
import org.springframework.web.client.RestClient;

import java.util.List;

@RequiredArgsConstructor
public class DefaultApiClient implements ApiClient {

    private final RestClient restClient;

    @Override
    public List<TaskDto> getTasks() {
        throw new UnsupportedOperationException("not implemented");
    }

    @Override
    public void createTask(String title, String description) {
        throw new UnsupportedOperationException("not implemented");
    }

    @Override
    public void deleteTask(Long id) {
        throw new UnsupportedOperationException("not implemented");
    }

    @Override
    public void completeTask(Long id) {
        throw new UnsupportedOperationException("not implemented");
    }
}
