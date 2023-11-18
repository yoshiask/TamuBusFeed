using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using OwlCore.ComponentModel;
using TamuBusFeed.Models;

namespace TamuBusFeed;

public class TamuBusFeedApi : IAsyncInit
{
    static TamuBusFeedApi()
    {
        FlurlHttp.Configure(settings =>
        {
            settings.HttpClientFactory = new TAMUTransportHttpClientFactory();
        });
    }

    private const string HOST_BASE = "https://transport.tamu.edu";
    private static readonly Url FEED_URL = HOST_BASE.AppendPathSegments("BusRoutes.web");
    private HubConnection? _mapHub;
    private HubConnection? _timeHub;

    private static IFlurlRequest GetBase() => new FlurlRequest(FEED_URL);

    public bool IsInitialized { get; private set; }

    private HubConnection MapHub
    {
        get => _mapHub!;
        set => _mapHub = value;
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        MapHub = new HubConnectionBuilder()
            .WithUrl(FEED_URL.AppendPathSegment("mapHub"))
            .Build();

        MapHub.Closed += HandleMapHubError;

        IsInitialized = true;
    }

    public async Task<List<Route>> GetRoutes()
    {
        await InitAsync();

        var result = await MapHub.InvokeAsync<object>("GetRoute", "01");
        return new();
    }

    public async Task<List<PatternElement>> GetPattern(string shortname, DateTimeOffset? date = null)
    {
        date ??= DateTimeOffset.Now;
        return await GetBase()
            .AppendPathSegments("route", shortname, "pattern", date.Value.ToString("yyyy-MM-dd"))
            .GetJsonAsync<List<PatternElement>>();
    }

    public async Task<List<PatternElement>> GetStops(string shortname)
    {
        return await GetBase()
            .AppendPathSegments("route", shortname, "stops")
            .GetJsonAsync<List<PatternElement>>();
    }

    public async Task<AnnouncementFeed> GetAnnouncements()
    {
        return await GetBase()
            .AppendPathSegments("announcements")
            .GetJsonAsync<AnnouncementFeed>();
    }

    public async Task<TimeTable> GetTimetable(string shortname, DateTimeOffset? date = null)
    {
        date ??= DateTimeOffset.Now;
        var response = await GetBase()
            .AppendPathSegments("route", shortname, "TimeTable", date.Value.ToString("yyyy-MM-dd"))
            .GetJsonAsync<List<JObject>>();

        TimeTable timeTable = new()
        {
            TimeStops = new()
        };

        // Check for no service
        if (response.Count == 1 && response[0].ContainsKey(" "))
            return timeTable;

        // Add stops
        int guidLength = Guid.Empty.ToString().Length;
        foreach (var stop in response[0])
        {
            // The JSON keys look like "e6266125-1350-4226-8413-b41869f0c313Trigon"
            string name = stop.Key.Remove(0, guidLength);
            timeTable.TimeStops.Add(new()
            {
                Name = name,
                LeaveTimes = new()
            });
        }

        // Populate times
        foreach (var row in response)
        {
            var rowTimes = row.Children<JProperty>().Select(c => c.Value.ToString()).ToArray();
            for (int col = 0; col < row.Count; col++)
            {
                DateTimeOffset? time = null;
                string timeStr = rowTimes[col];
                if (!string.IsNullOrEmpty(timeStr))
                    time = DateTimeOffset.Parse(timeStr);

                timeTable.TimeStops[col].LeaveTimes.Add(time);
            }
        }

        return timeTable;
    }

    public async Task<List<Mentor>> GetVehicles(string shortname)
    {
        return await GetBase()
            .AppendPathSegments("route", shortname, "buses", "mentor")
            .GetJsonAsync<List<Mentor>>();
    }

    private Task HandleMapHubError(Exception? error) => HandleConnectionErrorAsync(_mapHub!, error);
    private Task HandleTimeHubError(Exception? error) => HandleConnectionErrorAsync(_timeHub!, error);

    private async Task HandleConnectionErrorAsync(HubConnection connection, Exception? error)
    {
        await Task.Delay(new Random().Next(0, 5) * 1000);
        await connection.StartAsync();
    }
}

public class TAMUTransportHttpClientFactory : DefaultHttpClientFactory
{
    // override to customize how HttpMessageHandler is created/configured
    public override HttpMessageHandler CreateMessageHandler()
    {
        HttpClientHandler httpClientHandler = new HttpClientHandler();

        // TODO: THIS IS VERY DANGEROUS! This callback makes it so that any SSL certificate is considered valid, even if it's
        // malicious or self-signed. This opens the app to man-in-the-middle attacks. See the following issue:
        // https://github.com/xamarin/xamarin-android/issues/4688
        httpClientHandler.ServerCertificateCustomValidationCallback += ValidateTAMUTransportSSLCert;
        return httpClientHandler;
    }

    private static bool ValidateTAMUTransportSSLCert(HttpRequestMessage sender, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicy)
    {
        bool isValid = certificate.Subject == "CN=transport.tamu.edu, OU=Texas A&M IT, O=Texas A & M University, STREET=112 Jack K Williams Admin Building, L=College Station, S=Texas, PostalCode=77843, C=US"
            || certificate.Subject == "CN=transport.tamu.edu, OU=Texas A&M IT, O=Texas A & M University, STREET=112 Jack K Williams Admin Building, L=College Station, S=Texas, OID.2.5.4.17=77843, C=US"
            || certificate.Subject == "CN=transport.tamu.edu, OU=Texas A&M IT, O=Texas A & M University, L=College Station, S=Texas, C=US";
        return isValid;
    }
}
