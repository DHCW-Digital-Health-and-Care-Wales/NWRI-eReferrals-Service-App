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
    private const string LoggerType = "UserActivityAuditLog";
    private const string TimestampKey = "EventTimestampUtc";
    private const string ExceptionTypeKey = "ExceptionType";
    private const string ExceptionMessageKey = "ExceptionMessage";

    public EventLogger(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void Audit(IAuditEvent auditEvent)
    {
        var eventName = GetName(auditEvent.GetType());
        _telemetryClient.TrackEvent(eventName, GenerateEventContext(auditEvent, AuditType));
    }

    public void LogError(IErrorEvent errorEvent, Exception exception)
    {
        var eventName = GetName(errorEvent.GetType());
        var properties = GenerateEventContext(errorEvent, ErrorType);

        properties[ExceptionTypeKey] = exception.GetType().Name;
        properties[ExceptionMessageKey] = exception.Message;

        _telemetryClient.TrackEvent(eventName, properties);
    }

    private static string GetName(Type type)
    {
        var namedAttribute = type.GetCustomAttribute<DescriptionAttribute>();
        return namedAttribute != null ? namedAttribute.Description : type.Name;
    }

    private static Dictionary<string, string> GenerateEventContext(IEvent sourceEvent, string eventType)
    {
        var properties = GetFieldsAsMap(sourceEvent);
        properties[LoggerTypeKey] = LoggerType;
        properties[EventTypeKey] = eventType;
        properties[TimestampKey] = DateTimeOffset.UtcNow.ToString("O");

        return properties;
    }

    private static Dictionary<string, string> GetFieldsAsMap(IEvent sourceEvent)
    {
        return sourceEvent.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(prop => prop.Name, prop => GetValue(sourceEvent, prop));
    }

    private static string GetValue(IEvent sourceEvent, PropertyInfo property)
    {
        var value = property.GetValue(sourceEvent);
        return value is null ? "N/A" : value.ToString() ?? "N/A";
    }
}
