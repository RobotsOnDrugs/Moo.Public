using System.Text.Json.Serialization;

namespace Moo.Notifications;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Notifier.NotificationData>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}