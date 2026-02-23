using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetSuiteIntegrationAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

public class NetSuiteService
{
    private readonly NetSuiteOptions _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _accessToken;
    private DateTime _tokenExpiry;
    private readonly IMemoryCache _cache;

    public NetSuiteService(
        IOptions<NetSuiteOptions> options,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache)
    {
        _config = options.Value;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    private string Account => _config.AccountId.ToLower().Replace("_", "-");
    private string ClientId => _config.ClientId;
    private string CertificateId => _config.CertificateId;
    private string TokenEndpoint => $"https://{Account}.suitetalk.api.netsuite.com/services/rest/auth/oauth2/v1/token";
    private string GenerateClientAssertion()
    {
        var now = DateTime.UtcNow;

        var privateKeyPem = _config.PrivateKey?
        .Replace("\\n", "\n")
        .Trim();

        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new Exception("NetSuite PrivateKey is not configured.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());

        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = CertificateId // MUST match OAuth2 Mapping Certificate ID
        };

        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSsaPssSha256);
        signingCredentials.Key.KeyId = CertificateId;

        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = ClientId,
            Audience = TokenEndpoint,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            IssuedAt = now,
            Claims = new Dictionary<string, object>
            {
                { "scope", new[] { "restlets", "rest_webservices" } },
                { "jti", Guid.NewGuid().ToString() }
            },
            SigningCredentials = signingCredentials
        };

        var token = handler.CreateToken(descriptor);
        var jwt = handler.WriteToken(token);

        return jwt;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        var clientAssertion = GenerateClientAssertion();


        var requestParams = new List<KeyValuePair<string, string>>();
        requestParams.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
        requestParams.Add(new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
        requestParams.Add(new KeyValuePair<string, string>("client_assertion", clientAssertion));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
        httpRequest.Content = new FormUrlEncodedContent(requestParams);

        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NetSuite Token Error: {content}");

        var json = JsonSerializer.Deserialize<NsToken>(content);

        if (json == null || string.IsNullOrWhiteSpace(json.AccessToken))
        {
            throw new Exception("Failed to deserialize NetSuite token response.");
        }

        _accessToken = json.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(json.ExpiresIn - 60);

        return _accessToken;
    }

    public async Task<string> GetAccessTokenForTestAsync()
    {
        return await GetAccessTokenAsync();
    }

    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync(
            "netsuite_navigation_categories",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await GetCommerceCategoriesAsync();
            }
        ) ?? new CategoriesResponse();
    }

    public async Task<CategoriesResponse> GetCommerceCategoriesAsync()
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();

        var url = $"https://{Account}.restlets.api.netsuite.com/app/site/hosting/restlet.nl" +
                  $"?script=customscript_restlet_categories" +
                  $"&deploy=customdeploy_restlet_categories";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NetSuite API Error: {content}");

        var result = JsonSerializer.Deserialize<CategoriesResponse>(
            content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result ?? new CategoriesResponse();
    }

    public async Task<CategoryItemsResponse> GetCategoryItemsAsync(string categoryId)
    {
        return await _cache.GetOrCreateAsync(
            $"netsuite_category_items_{categoryId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await GetCategoryItemsByIdAsync(categoryId);
            }
        ) ?? new CategoryItemsResponse();
    }

    public async Task<CategoryItemsResponse> GetCategoryItemsByIdAsync(string categoryId)
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();

        var url = $"https://{Account}.restlets.api.netsuite.com/app/site/hosting/restlet.nl" +
                  $"?script=2100" +
                  $"&deploy=1" +
                  $"&categoryId={categoryId}" +
                  $"&pageIndex=0" +
                  $"&pageSize=1000";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NetSuite API Error: {content}");

        var result = JsonSerializer.Deserialize<CategoryItemsResponse>(
            content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result ?? new CategoryItemsResponse();
    }

    public async Task<BulkProductResponse> GetProductsForIndexingAsync(int pageIndex, int pageSize)
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();

        var url = $"https://{Account}.restlets.api.netsuite.com/app/site/hosting/restlet.nl" +
                  $"?script=2102" +
                  $"&deploy=1" +
                  $"&pageIndex={pageIndex}" +
                  $"&pageSize={pageSize}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NetSuite API Error: {content}");

        return JsonSerializer.Deserialize<BulkProductResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;
    }
}