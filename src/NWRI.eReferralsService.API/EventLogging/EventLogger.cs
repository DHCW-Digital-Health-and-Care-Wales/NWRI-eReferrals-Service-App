using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.ApplicationInsights;
using NWRI.eReferralsService.API.EventLogging.Interfaces;

namespace NWRI.eReferralsService.API.EventLogging;

public sealed class EventLogger : IEventLogger
{
    private readonly TelemetryClient _telemetryClient;

    private const string AuditType = "Audit";
    private const string ErrorType = "Error";

    private const string EventTypeKey = "EventType";
    private const string LoggerTypeKey = "LoggerType";
    private const string LoggerType = "AuditLog";

    private const string ExceptionTypeKey = "ExceptionType";
    private const string ExceptionMessageKey = "ExceptionMessage";

    private readonly record struct EventTypeMetadata(string EventName, PropertyInfo[] Properties);
    private static readonly ConcurrentDictionary<Type, EventTypeMetadata> EventTypeMetadataCache = new();

    public EventLogger(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void Audit(IAuditEvent auditEvent)
    {
        var metadata = GetOrCreateMetadata(auditEvent.GetType());
        var properties = BuildEventProperties(auditEvent, metadata, AuditType);

        _telemetryClient.TrackEvent(metadata.EventName, properties);
    }

    public void LogError(IErrorEvent errorEvent, Exception? exception)
    {
        var metadata = GetOrCreateMetadata(errorEvent.GetType());
        var properties = BuildEventProperties(errorEvent, metadata, ErrorType);

        if (exception != null)
        {
            properties[ExceptionTypeKey] = exception.GetType().Name;
            properties[ExceptionMessageKey] = exception.Message;
        }

        _telemetryClient.TrackEvent(metadata.EventName, properties);
    }

    private static Dictionary<string, string> BuildEventProperties(
        IEvent sourceEvent,
        EventTypeMetadata metadata,
        string eventType)
    {
        var properties = ExtractEventProperties(sourceEvent, metadata);

        properties[LoggerTypeKey] = LoggerType;
        properties[EventTypeKey] = eventType;

        return properties;
    }

    private static Dictionary<string, string> ExtractEventProperties(
        IEvent sourceEvent,
        EventTypeMetadata metadata)
    {
        var properties = new Dictionary<string, string>(metadata.Properties.Length);
        foreach (var property in metadata.Properties)
        {
            properties[property.Name] = GetPropertyValue(sourceEvent, property);
        }

        return properties;
    }

    private static EventTypeMetadata GetOrCreateMetadata(Type type)
    {
        return EventTypeMetadataCache.GetOrAdd(type, static eventType =>
        {
            var descriptionAttribute = eventType.GetCustomAttribute<DescriptionAttribute>();
            var eventName = descriptionAttribute?.Description ?? eventType.Name;

            var properties = eventType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray();

            return new EventTypeMetadata(eventName, properties);
        });
    }

    private static string GetPropertyValue(IEvent sourceEvent, PropertyInfo property)
    {
        var value = property.GetValue(sourceEvent);
        return value?.ToString() ?? string.Empty;
    }
}
