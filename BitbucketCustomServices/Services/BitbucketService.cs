using System.Text;
using System.Text.Json;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Requests.Bitbucket;
using BitbucketCustomServices.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace BitbucketCustomServices.Services;

public sealed class BitbucketService : IBitbucketService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BitbucketService> _logger;

    public BitbucketService(IHttpClientFactory httpClientFactory,
        ILogger<BitbucketService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<HttpClient> GetAuthenticatedClient(RepositoryCredentials credentials)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        httpClient.DefaultRequestHeaders.Authorization = credentials.AuthType switch
        {
            AuthType.Basic => new("Basic", Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{credentials.Username?.Trim() ?? ""}:{credentials.Password?.Trim() ?? ""}"))),
            AuthType.AuthToken => new("Bearer", credentials.Token?.Trim() ?? ""),
            _ => throw new ArgumentOutOfRangeException(nameof(credentials.AuthType),
                $"Unknown auth type: {credentials.AuthType}")
        };

        return Task.FromResult(httpClient);
    }

    public async Task<PullRequest> CreatePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        string sourceBranch,
        string targetBranch,
        string title,
        string description)
    {
        var prUrl = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests";
        var prPayload = new PullRequestCreateRequest(
            Title: title,
            Source: new BranchReference(sourceBranch),
            Destination: new BranchReference(targetBranch),
            Description: description);

        var prResponse = await client.PostAsJsonAsync(prUrl, prPayload);
        if (!prResponse.IsSuccessStatusCode)
        {
            _logger.LogError("PR creation failed for {SourceBranch} → {TargetBranch}: {Reason}",
                sourceBranch, targetBranch, prResponse.ReasonPhrase);

            throw new HttpRequestException($"Failed to create PR: {prResponse.ReasonPhrase}");
        }

        var createdPr = await JsonSerializer.DeserializeAsync<PullRequest>(
            await prResponse.Content.ReadAsStreamAsync());

        if (createdPr == null)
        {
            throw new InvalidOperationException("Failed to parse PR response");
        }

        return createdPr;
    }

    public async Task<bool> MergePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        int pullRequestId,
        string message,
        string mergeStrategy)
    {
        var mergeUrl =
            $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestId}/merge";
        var mergePayload = new PullRequestMergeRequest(
            Message: message,
            MergeStrategy: mergeStrategy);

        var mergeResponse = await client.PostAsJsonAsync(mergeUrl, mergePayload);

        return mergeResponse.IsSuccessStatusCode;
    }

    public async Task<bool> CreateBranch(
        HttpClient client,
        RepositoryCredentials credentials,
        string workspace,
        string repoSlug,
        string branchName,
        string commitHash)
    {
        var branchUrl = $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/refs/branches";
        var branchPayload = new BranchCreateRequest(
            Name: branchName,
            Target: new CommitReference(commitHash));

        var branchResponse = await client.PostAsJsonAsync(branchUrl, branchPayload);

        if (branchResponse.IsSuccessStatusCode) 
            return branchResponse.IsSuccessStatusCode;
        
        var reason = await branchResponse.Content.ReadAsStringAsync();
        _logger.LogWarning("Branch creation failed for {BranchName}. HttpStatus: {HttpStatus}, Reason: {Reason}", branchName, branchResponse.StatusCode, reason);

        var jObject = JObject.Parse(reason);
        if (jObject["error"]?["code"]?.ToString() != "BRANCH_PERMISSION_VIOLATED")
            return branchResponse.IsSuccessStatusCode;

        using var fallbackClient = await GetAuthenticatedClient(credentials);
        branchResponse = await fallbackClient.PostAsJsonAsync(branchUrl, branchPayload);

        if (branchResponse.IsSuccessStatusCode)
            return branchResponse.IsSuccessStatusCode;

        reason = await branchResponse.Content.ReadAsStringAsync();
        _logger.LogError("Branch creation failed for {BranchName}. HttpStatus: {HttpStatus}, Reason: {Reason}", branchName, branchResponse.StatusCode, reason);

        return branchResponse.IsSuccessStatusCode;
    }

    public async Task<bool> UpdatePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        int pullRequestId,
        string sourceBranch,
        string title)
    {
        var updateUrl =
            $"https://api.bitbucket.org/2.0/repositories/{workspace}/{repoSlug}/pullrequests/{pullRequestId}";
        var updatePayload = new PullRequestUpdateRequest(
            Source: new BranchReference(sourceBranch),
            Title: title);

        var updateResponse = await client.PutAsJsonAsync(updateUrl, updatePayload);

        return updateResponse.IsSuccessStatusCode;
    }
}