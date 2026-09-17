using System.Text.Json;

namespace Tinycast.Windows;

sealed class JsonStore<T> where T : new()
{
    readonly string _path;
    readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonStore(string path) => _path = path;

    public T Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<T>(File.ReadAllText(_path), _options) ?? new T();
        }
        catch { /* a corrupt file starts empty rather than crashing the palette */ }
        return new T();
    }

    public void Save(T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, _options));
        File.Move(tmp, _path, overwrite: true);
    }
}

static class AppPaths
{
    public static string Root
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tinycast");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string Settings => Path.Combine(Root, "settings.json");
    public static string Quicklinks => Path.Combine(Root, "quicklinks.json");
    public static string Snippets => Path.Combine(Root, "snippets.json");
    public static string Notes => Path.Combine(Root, "notes.json");
    public static string Clipboard => Path.Combine(Root, "clipboard.json");
    public static string Secret => Path.Combine(Root, "mistral.key");
}

static class SecretStore
{
    public static string? LoadMistralKey()
    {
        try
        {
            if (!File.Exists(AppPaths.Secret)) return null;
            var bytes = File.ReadAllBytes(AppPaths.Secret);
            if (OperatingSystem.IsWindows())
            {
                var plain = System.Security.Cryptography.ProtectedData.Unprotect(
                    bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(plain);
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch { return null; }
    }

    public static void SaveMistralKey(string key)
    {
        var plain = System.Text.Encoding.UTF8.GetBytes(key);
        byte[] stored = plain;
        if (OperatingSystem.IsWindows())
        {
            stored = System.Security.Cryptography.ProtectedData.Protect(
                plain, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        }
        File.WriteAllBytes(AppPaths.Secret, stored);
    }
}
