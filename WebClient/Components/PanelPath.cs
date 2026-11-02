namespace WebClient.Components;

public static class PanelPath
{
    public static string Combine(string prefix, int id) => prefix + "/" + id.ToString();

    public static bool Contains(string path, int id)
    {
        var token = "/" + id.ToString();
        return path.EndsWith(token, StringComparison.Ordinal)
            || path.Contains(token + "/", StringComparison.Ordinal);
    }
}
