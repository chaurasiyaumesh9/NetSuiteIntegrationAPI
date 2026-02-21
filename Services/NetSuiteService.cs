using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetSuiteIntegrationAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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

    private string RestBaseUrl => $"https://{Account}.suitetalk.api.netsuite.com/services/rest";

    // ==========================================================
    // Generate Client Assertion (JWT)
    // ==========================================================
    private string GenerateClientAssertion()
    {
        var now = DateTime.UtcNow;

        var privateKeyPem = _config.PrivateKey;

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

    // ==========================================================
    // Get OAuth Access Token
    // ==========================================================
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

        _accessToken = json.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(json.ExpiresIn - 60);

        return _accessToken;
    }

    // ==========================================================
    // Commerce Categories
    // ==========================================================
    public async Task<List<SuiteQlCategoryRow>> GetCommerceCategoriesAsync()
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{RestBaseUrl}/query/v1/suiteql"
        );

        var queryObject = new
        {
            q = @"
            SELECT
                c.id,
                c.name,
                c.primaryparent,
                c.urlfragment,
                f.url AS imageurl
            FROM CommerceCategory c
            LEFT JOIN File f
                ON c.thumbnail = f.id
            WHERE c.isinactive = 'F'
              AND c.displayinsite = 'T'
        "
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(queryObject),
            Encoding.UTF8,
            "application/json"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add("Prefer", "transient");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NetSuite API Error: {content}");

        var result = JsonSerializer.Deserialize<SuiteQlResult<SuiteQlCategoryRow>>(
            content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result?.Items ?? new List<SuiteQlCategoryRow>();
    }

    // For testing only
    public async Task<string> GetAccessTokenForTestAsync()
    {
        return await GetAccessTokenAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        return await _cache.GetOrCreateAsync("netsuite_navigation_categories", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var rows = await GetCommerceCategoriesAsync();
            return TransformToAngularModel(rows);
        }) ?? new List<CategoryDto>();
    }

    private string CleanName(string input)
    {
        return input
            .Replace("\\\"", "")
            .Replace("\"", "")
            .Trim();
    }

    private string? BuildImageUrl(string? thumbnailId)
    {
        if (string.IsNullOrEmpty(thumbnailId))
            return null;

        var accountUpper = _config.AccountId.ToUpper();

        return $"https://{_config.AccountId.ToLower()}.app.netsuite.com/core/media/media.nl?id={thumbnailId}&c={accountUpper}";
    }

    private List<CategoryDto> TransformToAngularModel(List<SuiteQlCategoryRow> rows)
    {
        var lookup = rows.ToDictionary(
            r => r.Id,
            r =>
            {
                var cleanedName = CleanName(r.Name);

                return new CategoryDto
                {
                    Id = GenerateSlug(cleanedName),
                    Name = cleanedName,
                    Slug = r.UrlFragment ?? GenerateSlug(cleanedName),
                    Image = r.ImageUrl != null
                        ? $"https://{_config.AccountId.ToLower()}.app.netsuite.com{r.ImageUrl}"
                        : null
                };
            });

        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.PrimaryParent) &&
                lookup.ContainsKey(row.PrimaryParent))
            {
                lookup[row.PrimaryParent]
                    .SubCategories
                    .Add(lookup[row.Id]);
            }
        }

        var roots = rows
            .Where(r => string.IsNullOrEmpty(r.PrimaryParent))
            .Select(r => lookup[r.Id])
            .ToList();

        foreach (var root in roots)
        {
            BuildUrls(root, null);
        }

        return roots;
    }

    private void BuildUrls(CategoryDto node, string? parentUrl)
    {
        node.Url = parentUrl == null
            ? $"/{node.Slug}"
            : $"{parentUrl}/{node.Slug}";

        foreach (var child in node.SubCategories)
        {
            BuildUrls(child, node.Url);
        }
    }

    private string GenerateSlug(string input)
    {
        return input
            .ToLowerInvariant()
            .Replace("&", "and")
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}