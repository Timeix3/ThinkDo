package com.jedi.tasktracker.config;

import com.jedi.tasktracker.client.DefaultApiClient;
import com.jedi.tasktracker.security.OAuthClientHttpRequestInterceptor;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.oauth2.client.registration.ClientRegistrationRepository;
import org.springframework.security.oauth2.client.web.DefaultOAuth2AuthorizedClientManager;
import org.springframework.security.oauth2.client.web.OAuth2AuthorizedClientRepository;
import org.springframework.web.client.RestClient;

@Configuration
public class ClientBeans {

    @Bean
    public DefaultApiClient apiClient(
            @Value("${thinkdo.api.base-url:http://localhost:8081}")
            String apiBaseUrl,
            ClientRegistrationRepository clientRegistrationRepository,
            OAuth2AuthorizedClientRepository authorizedClientRepository) {
        OAuthClientHttpRequestInterceptor oAuthClientHttpRequestInterceptor =
                new OAuthClientHttpRequestInterceptor(
                        new DefaultOAuth2AuthorizedClientManager(clientRegistrationRepository,
                                authorizedClientRepository), "github");

        var restClientBuilder = RestClient.builder()
                .requestInterceptor(oAuthClientHttpRequestInterceptor);

        return new DefaultApiClient(restClientBuilder.baseUrl(apiBaseUrl)
                .build());
    }
}