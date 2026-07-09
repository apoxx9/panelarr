using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Notifications.Kavita;

public interface IKavitaServiceProxy
{
    string GetBaseUrl(KavitaSettings settings, string relativePath = null);
    void Notify(KavitaSettings settings, string message);
    string GetToken(KavitaSettings settings);
    ReaderPushResult PushCbl(KavitaSettings settings, string fileName, byte[] cblData);
}

public class KavitaServiceProxy : IKavitaServiceProxy
{
    private readonly IHttpClient _httpClient;
    private readonly Logger _logger;

    public KavitaServiceProxy(IHttpClient httpClient, Logger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string GetBaseUrl(KavitaSettings settings, string relativePath = null)
    {
        var baseUrl = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, string.Empty);
        baseUrl = HttpUri.CombinePath(baseUrl, relativePath);

        return baseUrl;
    }

    public void Notify(KavitaSettings settings, string folderPath)
    {
        var request = GetKavitaServerRequest("library/scan-folder", HttpMethod.Post, settings);
        request.Headers.ContentType = "application/json";
        var postRequest = request.Build();
        postRequest.SetContent(new
        {
            ApiKey = settings.ApiKey,
            FolderPath = folderPath
        }.ToJson());

        var response = _httpClient.Post(postRequest);
        _logger.Trace("Update response: {0}", string.IsNullOrEmpty(response.Content) ? "Success" : response.Content);
    }

    public string GetToken(KavitaSettings settings)
    {
        var request = GetKavitaServerRequest("plugin/authenticate", HttpMethod.Post, settings);
        request.AddQueryParam("apiKey", settings.ApiKey)
            .AddQueryParam("pluginName", BuildInfo.AppName);
        var response = _httpClient.Execute(request.Build());

        _logger.Trace("Authenticate response: {0}", response.Content);

        var authResult = JsonSerializer.Deserialize<KavitaAuthenticationResult>(response.Content);

        if (authResult == null)
        {
            throw new KavitaException("Could not authenticate with Kavita");
        }

        return authResult.Token;
    }

    public ReaderPushResult PushCbl(KavitaSettings settings, string fileName, byte[] cblData)
    {
        var token = GetToken(settings);

        KavitaCblSavedFile savedFile;

        try
        {
            var request = GetAuthenticatedRequest("cbl/file-import", settings, token);
            request.AddFormUpload("cblFile", fileName, cblData, "application/xml");

            savedFile = Deserialize<KavitaCblSavedFile>(_httpClient.Execute(request.Build()));
        }
        catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            // Kavita < 0.9 has no cbl/file-import; it does the whole import
            // in one shot.
            return PushCblLegacy(settings, token, fileName, cblData);
        }

        // v0.9+ flow: re-validate reports matching, then finalize-import (with
        // no manual decisions) imports every auto-matched item and updates a
        // same-name list in place.
        var validateRequest = GetAuthenticatedRequest("cbl/re-validate", settings, token);
        validateRequest.Headers.ContentType = "application/json";
        var validatePost = validateRequest.Build();
        validatePost.SetContent(new { FileName = savedFile.FileName }.ToJson());

        var validation = Deserialize<KavitaCblImportSummary>(_httpClient.Execute(validatePost));

        if (validation.Success == KavitaCblImportSummary.ResultFail)
        {
            throw new KavitaException($"Kavita rejected the reading list: {DescribeFailures(validation)}");
        }

        var finalizeRequest = GetAuthenticatedRequest("cbl/finalize-import", settings, token);
        finalizeRequest.Headers.ContentType = "application/json";
        var finalizePost = finalizeRequest.Build();
        finalizePost.SetContent(new
        {
            FileName = savedFile.FileName,
            Decisions = new { ItemResolutions = new { }, SaveAsRemapRules = false },
            Provider = savedFile.Provider,
            Promote = false
        }.ToJson());

        var summary = Deserialize<KavitaCblImportSummary>(_httpClient.Execute(finalizePost));

        if (summary.Success == KavitaCblImportSummary.ResultFail)
        {
            throw new KavitaException($"Kavita rejected the reading list: {DescribeFailures(summary)}");
        }

        return ToPushResult(summary);
    }

    private ReaderPushResult PushCblLegacy(KavitaSettings settings, string token, string fileName, byte[] cblData)
    {
        var request = GetAuthenticatedRequest("cbl/import", settings, token);
        request.AddQueryParam("dryRun", "false");
        request.AddQueryParam("useComicVineMatching", "true");
        request.AddFormUpload("cbl", fileName, cblData, "application/xml");

        var summary = Deserialize<KavitaCblImportSummary>(_httpClient.Execute(request.Build()));

        if (summary.Success == KavitaCblImportSummary.ResultFail)
        {
            throw new KavitaException($"Kavita rejected the reading list: {DescribeFailures(summary)}");
        }

        return ToPushResult(summary);
    }

    private static ReaderPushResult ToPushResult(KavitaCblImportSummary summary)
    {
        return new ReaderPushResult
        {
            Updated = summary.IsUpdate,
            MatchedCount = summary.SuccessfulInserts?.Count ?? 0,
            Unmatched = summary.Results?
                .Where(r => r.Reason != KavitaCblBookResult.ReasonSuccess)
                .Select(r => r.Describe())
                .ToList() ?? new List<string>()
        };
    }

    private static string DescribeFailures(KavitaCblImportSummary summary)
    {
        var failures = summary.Results?
            .Where(r => r.Reason != KavitaCblBookResult.ReasonSuccess)
            .Select(r => r.Describe())
            .Take(3)
            .ToList();

        return failures?.Any() == true ? string.Join("; ", failures) : "no reason given";
    }

    private static T Deserialize<T>(HttpResponse response)
    {
        var result = JsonSerializer.Deserialize<T>(response.Content);

        if (result == null)
        {
            throw new KavitaException("Kavita returned an empty response");
        }

        return result;
    }

    private HttpRequestBuilder GetAuthenticatedRequest(string resource, KavitaSettings settings, string token)
    {
        var request = GetKavitaServerRequest(resource, HttpMethod.Post, settings);
        request.Headers["Authorization"] = $"Bearer {token}";

        return request;
    }

    private HttpRequestBuilder GetKavitaServerRequest(string resource, HttpMethod method, KavitaSettings settings)
    {
        var client = new HttpRequestBuilder(GetBaseUrl(settings, "api"));

        client.Resource(resource);

        if (settings.ApiKey.IsNotNullOrWhiteSpace())
        {
            client.Headers["x-kavita-apikey"] = settings.ApiKey;
            client.Headers["x-kavita-plugin"] = BuildInfo.AppName;
        }

        client.Method = method;

        return client;
    }
}
