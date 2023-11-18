using Flurl.Http;
using Flurl.Http.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamuBusFeed;

public class Connection
{
    const string USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4585.0 Safari/537.36";

    private readonly string _baseUrl;
    private readonly Dictionary<string, object> _pendingRequests = new();
    private readonly IFlurlClient _httpClient;
    private FlurlCookie? _sessionCookie;
    private string? _connectionToken;

    private Connection(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public Connection(string baseUrl, IFlurlClient httpClient) : this(baseUrl)
    {
        _httpClient = httpClient;
    }

    public Connection(string baseUrl, IFlurlClientFactory httpClientFactory) : this(baseUrl)
    {
        _httpClient = httpClientFactory.Get(baseUrl);
    }

    public async Task ConnectAsync()
    {
        // Get Session ID for authentication
        var sessionIdResponse = await _httpClient
            .Request("busroutes.web")
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithHeader("User-Agent", USER_AGENT)
            .GetAsync();

        _sessionCookie = sessionIdResponse.Cookies.FirstOrDefault();

        // Negotiate connection to server
        var negotiateResponse = await Request("negotiate")
            .SetQueryParam("negotiateVersion", 1)
            .PostAsync();

        // Extract connection token
        _connectionToken = (await negotiateResponse.GetJsonAsync()).connectionToken;

        // Connect to SSE server

    }

    private IFlurlRequest Request(params string[] urlSegments)
    {
        var request = _httpClient
            .Request(urlSegments)
            .WithHeader("User-Agent", USER_AGENT);

        if (_sessionCookie is not null)
            request = request.WithCookie(_sessionCookie.Name, _sessionCookie.Value);

        return request;
    }
}
