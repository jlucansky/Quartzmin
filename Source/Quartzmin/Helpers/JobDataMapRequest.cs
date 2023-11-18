using FormFile = Quartzmin.Models.FormFile;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Quartzmin.Helpers;

internal static class JobDataMapRequest
{
    public static Task<Dictionary<string, object>[]> GetJobDataMapFormAsync(
        this IEnumerable<KeyValuePair<string, object>> formData,
        bool includeRowIndex = true)
    {
        // key1 is group, key2 is field
        var map = new Dictionary<string, Dictionary<string, object>>();

        foreach (var item in formData)
        {
            string g = GetJobDataMapFieldGroup(item.Key);
            if (g != null)
            {
                string field = item.Key.Substring(0, item.Key.Length - g.Length - 1);
                if (!map.ContainsKey(g))
                    map[g] = new Dictionary<string, object>();
                map[g][field] = item.Value;
            }
        }

        if (includeRowIndex)
        {
            foreach (var g in map.Keys)
            {
                map[g]["data-map[index]"] = g;
            }
        }

        return Task.FromResult(map.Values.ToArray());
    }

    public static async Task<Dictionary<string, object>[]> GetJobDataMapFormAsync(
        this HttpRequest request,
        bool includeRowIndex = true)
    {
        return await GetJobDataMapFormAsync(await request.GetFormDataAsync(), includeRowIndex);
    }

    private static string GetJobDataMapFieldGroup(string field)
    {
        var n = field.LastIndexOf(':');
        if (n == -1)
            return null;

        return field.Substring(n + 1);
    }

    public static Task<List<KeyValuePair<string, object>>> GetFormDataAsync(this HttpRequest request)
    {
        var result = new List<KeyValuePair<string, object>>();

        foreach (var key in request.Form.Keys)
        {
            foreach (string strValue in request.Form[key])
            {
                result.Add(new KeyValuePair<string, object>(key, strValue));
            }
        }

        foreach (var file in request.Form.Files)
        {
            result.Add(new KeyValuePair<string, object>(file.Name, new FormFile(file)));
        }

        return Task.FromResult(result);
    }
}