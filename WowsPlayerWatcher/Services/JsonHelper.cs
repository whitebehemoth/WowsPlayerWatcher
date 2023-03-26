using System.Text.Json;

namespace WowsPlayerWatcher.Services
{
    public static class JsonHelper
    {
        public static T LoadJson<T>(string filename) where T : new()
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
            else
            {
                T obj = new T();
                SaveJson(filename, obj);
                return obj;
            }
        }
        public static void SaveJson<T>(string filename, T obj, bool overridefile = true)
        {
            var jsonSettingToSave = JsonSerializer.Serialize(obj);
            if (overridefile)
            {
                File.WriteAllText(filename, jsonSettingToSave);
            }
            else
            {
                File.AppendAllText(filename, jsonSettingToSave);
            }
        }
    }
}
