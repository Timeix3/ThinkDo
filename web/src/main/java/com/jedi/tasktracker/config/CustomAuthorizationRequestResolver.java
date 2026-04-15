package com.jedi.tasktracker.config;

import jakarta.servlet.http.HttpServletRequest;
import java.util.HashMap;
import java.util.Map;
import org.springframework.security.oauth2.client.registration.ClientRegistrationRepository;
import org.springframework.security.oauth2.client.web.DefaultOAuth2AuthorizationRequestResolver;
import org.springframework.security.oauth2.client.web.OAuth2AuthorizationRequestResolver;
import org.springframework.security.oauth2.core.endpoint.OAuth2AuthorizationRequest;

public class CustomAuthorizationRequestResolver implements OAuth2AuthorizationRequestResolver {

  private final DefaultOAuth2AuthorizationRequestResolver defaultResolver;

  public CustomAuthorizationRequestResolver(
      ClientRegistrationRepository clientRegistrationRepository) {
    this.defaultResolver =
        new DefaultOAuth2AuthorizationRequestResolver(
            clientRegistrationRepository, "/oauth2/authorization");
  }

  @Override
  public OAuth2AuthorizationRequest resolve(HttpServletRequest request) {
    OAuth2AuthorizationRequest authRequest = defaultResolver.resolve(request);
    return addPromptLogin(authRequest);
  }

  @Override
  public OAuth2AuthorizationRequest resolve(
      HttpServletRequest request, String clientRegistrationId) {
    OAuth2AuthorizationRequest authRequest = defaultResolver.resolve(request, clientRegistrationId);
    return addPromptLogin(authRequest);
  }

  private OAuth2AuthorizationRequest addPromptLogin(OAuth2AuthorizationRequest authRequest) {
    if (authRequest == null) return null;
    Map<String, Object> additionalParameters = new HashMap<>(authRequest.getAdditionalParameters());

    // Пытаемся заставить GitHub спросить логин/пароль
    additionalParameters.put("prompt", "login");
    additionalParameters.put("force_login", "true");

    return OAuth2AuthorizationRequest.from(authRequest)
        .additionalParameters(additionalParameters)
        .build();
  }
}
