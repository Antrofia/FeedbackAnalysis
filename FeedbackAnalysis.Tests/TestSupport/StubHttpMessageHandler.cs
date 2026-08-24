using System.Net;
using System.Text;

namespace FeedbackAnalysis.Tests.TestSupport;

/// <summary>
/// Подставной HttpMessageHandler: запоминает все запросы (URL + тело)
/// и отвечает через переданную фабрику ответов.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        _responder = responder ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
    }

    /// <summary>Позволяет задать/заменить фабрику ответов после создания.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage> DefaultResponder
    {
        get => _responder;
        set => _responder = value;
    }

    public List<string> RequestUris { get; } = [];

    public List<string?> RequestBodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);

        if (request.Content is null)
        {
            RequestBodies.Add(null);
        }
        else
        {
            RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return _responder(request);
    }
}
