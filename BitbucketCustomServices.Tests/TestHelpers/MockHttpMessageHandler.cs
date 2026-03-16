using System.Net;

namespace BitbucketCustomServices.Tests.TestHelpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Match, HttpResponseMessage Response)> _handlers = [];

    public void Setup(Func<HttpRequestMessage, bool> match, HttpResponseMessage response)
    {
        _handlers.Add((match, response));
    }

    public void Setup(Func<HttpRequestMessage, bool> match, HttpStatusCode statusCode, string? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
            response.Content = new StringContent(content);
        _handlers.Add((match, response));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        foreach (var (match, response) in _handlers)
        {
            if (match(request))
                return Task.FromResult(response);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
