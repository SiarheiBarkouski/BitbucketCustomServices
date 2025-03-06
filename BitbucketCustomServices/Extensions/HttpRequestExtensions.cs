using System.Reflection;
using System.Runtime.Serialization;
using BitbucketCustomServices.Enums;

namespace BitbucketCustomServices.Extensions;

public static class HttpRequestExtensions
{
    private static readonly Dictionary<string, EventType> _eventTypes = GetDictionary();

    private static Dictionary<string, EventType> GetDictionary()
    {
        var dictionary = Enum.GetValues<EventType>().ToDictionary(eventType =>
        {
            var bitbucketEventType = typeof(EventType).GetField(eventType.ToString()).GetCustomAttributes<EnumMemberAttribute>().FirstOrDefault()?.Value;

            if (bitbucketEventType is null)
                return eventType.ToString();

            return bitbucketEventType;
        }, evt => evt);

        return dictionary;
    }

    public static EventType GetEventType(this HttpRequest request)
    {
        var eventType = request.Headers["X-Event-Key"].FirstOrDefault()?.Trim();

        if (eventType is null || !_eventTypes.TryGetValue(eventType, out var result))
            return EventType.Default;

        return result;
    }
}