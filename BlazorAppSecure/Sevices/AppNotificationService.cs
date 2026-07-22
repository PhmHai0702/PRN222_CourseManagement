using AntDesign;
using System.Text.Json;

namespace BlazorAppSecure.Sevices
{
    public interface IAppNotificationService
    {
        Task Error(string message);
        Task Success(string message);
        Task Warning(string message);
        Task Info(string message);
    }

    public class AppNotificationService : IAppNotificationService
    {
        private readonly NotificationService _notification;

        public AppNotificationService(NotificationService notification)
        {
            _notification = notification;
        }

        public Task Error(string message)
        {
            return _notification.Error(BuildConfig("Error", message));
        }

        public Task Success(string message)
        {
            return _notification.Success(BuildConfig("Success", message));
        }

        public Task Warning(string message)
        {
            return _notification.Warning(BuildConfig("Warning", message));
        }

        public Task Info(string message)
        {
            return _notification.Info(BuildConfig("Info", message));
        }

        private static NotificationConfig BuildConfig(string title, string message)
        {
            return new NotificationConfig
            {
                Message = title,
                Description = NormalizeMessage(message),
                Duration = 0
            };
        }

        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var trimmed = message.Trim();
            if (trimmed.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("ERR_CONNECTION_REFUSED", StringComparison.OrdinalIgnoreCase))
            {
                return "Không kết nối được tới API. Hãy chạy CourseManagementAPI ở https://localhost:7239 rồi tải lại trang.";
            }

            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                return trimmed;
            }

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetString(root, "error", out var error))
                    {
                        return error ?? trimmed;
                    }

                    if (TryGetString(root, "message", out var friendlyMessage))
                    {
                        return friendlyMessage ?? trimmed;
                    }

                    if (TryGetFirstArrayValue(root, "errors", out var errors))
                    {
                        return errors ?? trimmed;
                    }

                    if (TryGetFirstArrayValue(root, "Errors", out var capitalErrors))
                    {
                        return capitalErrors ?? trimmed;
                    }
                }
            }
            catch
            {
                return trimmed;
            }

            return trimmed;
        }

        private static bool TryGetString(JsonElement root, string propertyName, out string? value)
        {
            value = null;
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }

            value = property.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetFirstArrayValue(JsonElement root, string propertyName, out string? value)
        {
            value = null;
            if (!root.TryGetProperty(propertyName, out var property) ||
                property.ValueKind != JsonValueKind.Array ||
                property.GetArrayLength() == 0)
            {
                return false;
            }

            var first = property[0];
            value = first.ValueKind == JsonValueKind.String ? first.GetString() : first.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
