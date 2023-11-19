using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
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
    private static readonly string FEED_URL = $"{HOST_BASE}/BusRoutes.web/";
    private HubConnection? _mapHub;
    private HubConnection? _timeHub;

    private static IFlurlRequest GetBase() => new FlurlRequest(FEED_URL);

    public bool IsInitialized { get; private set; }

    private HubConnection MapHub
    {
        get => _mapHub!;
        set => _mapHub = value;
    }

    private HubConnection TimeHub
    {
        get => _timeHub!;
        set => _timeHub = value;
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        MapHub = new HubConnectionBuilder()
            .WithUrl(FEED_URL + "mapHub")
            .Build();
        MapHub.Closed += HandleMapHubError;
        await MapHub.StartAsync(cancellationToken);

        TimeHub = new HubConnectionBuilder()
            .WithUrl(FEED_URL + "timeHub")
            .Build();
        TimeHub.Closed += HandleTimeHubError;
        await TimeHub.StartAsync(cancellationToken);

        IsInitialized = true;
    }

    public async Task<List<RouteInfo>> GetRoutesByGroup(string groupId)
    {
        await InitAsync();

        var routes = await MapHub.InvokeAsync<List<RouteInfo>>("GetRoutesByGroup", groupId);
        return routes;
    }

    public async IAsyncEnumerable<RouteInfo> GetAllRoutes()
    {
        await InitAsync();

        var allGroups = new string[] { "OnCampus", "OffCampus", "Gameday" };
        foreach (var groupId in allGroups)
        {
            var routes = await MapHub.InvokeAsync<List<RouteInfo>>("GetRoutesByGroup", groupId);
            foreach (var route in routes)
                yield return route;
        }
    }

    public async Task<RouteInfo> GetRoute(string routeNum)
    {
        await InitAsync();

        var route = await MapHub.InvokeAsync<RouteInfo>("GetRoute", routeNum);
        return route;
    }

    public async Task<Pattern> GetPatternPaths(string routeKey)
    {
        await InitAsync();

        var pattern = await MapHub.InvokeAsync<Pattern>("GetPatternPaths", routeKey);
        return pattern;
    }

    public async Task GetRoutePatterns(string routeKey)
    {
        throw new NotImplementedException();
    }

    public async Task<AnnouncementFeed> GetAnnouncements()
    {
        return await GetBase()
            .AppendPathSegments("announcements")
            .GetJsonAsync<AnnouncementFeed>();
    }

    public async Task<List<TimeTable>> GetTimetable(string routeNumber, DateTimeOffset? date = null)
    {
        await InitAsync();

        var dateString = (date ?? DateTimeOffset.Now).ToString("yyyy-MM-dd");

        var response = await TimeHub.InvokeAsync<JsonObject>("GetTimeTable", routeNumber, dateString);
        var jsonTimeTableList = response["jsonTimeTableList"]!.AsArray();

        List<TimeTable> timeTables = new(2);
        foreach (var directionNode in jsonTimeTableList)
        {
            var direction = directionNode!.AsObject();
            string destination = direction["destination"]!.GetValue<string>();
            if (destination.Length == 0)
                break;

            TimeTable timeTable = new()
            {
                Destination = destination
            };
            
            string html = "<table>" + direction["html"]!.GetValue<string>() + "</table>";
            var htmlDoc = await new HtmlParser().ParseDocumentAsync(html);
            foreach (var timeStopName in htmlDoc.QuerySelectorAll("thead > tr > th"))
            {
                timeTable.TimeStops.Add(new()
                {
                    Name = timeStopName!.TextContent
                });
            }

            var stopTimeEntries = htmlDoc.QuerySelectorAll("time").ToArray();
            var stopCount = timeTable.TimeStops.Count;
            for (int t = 0; t < stopTimeEntries.Length; t++)
            {
                var stopTimeEntry = stopTimeEntries[t];
                var stopTime = DateTimeOffset.Parse(stopTimeEntry.GetAttribute("dateTime")!);
                timeTable.TimeStops[t % stopCount].LeaveTimes.Add(stopTime);
            }

            timeTables.Add(timeTable);
        }

        return timeTables;
    }

    public async Task<List<Mentor>> GetBusses(string routeKey)
    {
        return new();
    }

    private Task HandleMapHubError(Exception? error) => HandleConnectionErrorAsync(MapHub, error);
    private Task HandleTimeHubError(Exception? error) => HandleConnectionErrorAsync(TimeHub, error);

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
