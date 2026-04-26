using System.Text.Json;

namespace StudyApp.Utilities;

public static class CloneHelper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? Clone<T>(T? value)
    {
        if (value is null)
        {
            return default;
        }

        var json = JsonSerializer.Serialize(value, Options);
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
