using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AdminApi.Services;

public class GitHubAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubAuthenticationHandler> _logger;

    public GitHubAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IHttpClientFactory httpClientFactory) : base(options, logger, encoder, clock)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger.CreateLogger<GitHubAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization header format. Expected: Bearer <token>");
        }

        var token = authHeader["Bearer ".Length..].Trim();

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Token is empty");
        }

        try
        {
            // Validate token by calling GitHub's user API
            var httpClient = _httpClientFactory.CreateClient("GitHub");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

            var response = await httpClient.GetAsync("https://api.github.com/user");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub token validation failed. Status: {StatusCode}", response.StatusCode);
                return AuthenticateResult.Fail("Invalid GitHub token");
            }

            var content = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<GitHubUserResponse>(content);

            if (user is null || string.IsNullOrEmpty(user.Login))
            {
                _logger.LogWarning("Failed to deserialize GitHub user data. Response: {Content}", content);
                return AuthenticateResult.Fail("Invalid user data from GitHub API");
            }

            // Create claims from GitHub user data
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim("github:avatar", user.AvatarUrl ?? string.Empty),
                new Claim("github:email", user.Email ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GitHub token");
            return AuthenticateResult.Fail("Token validation error");
        }
    }
}

public class GitHubUserResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
