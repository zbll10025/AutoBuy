using BepInEx;
using Newtonsoft.Json;
using System;
using System.IO;
using UDebug = UnityEngine.Debug;

namespace AutoBuy
{
    public static class JsonHelper
    {
        public static void Save<T>(T data, string fileName)
        {
            string filePath = GetPluginPath(fileName);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, json);
        }
        public static T Load<T>(T defaultValue, string fileName)
        {

            string filePath = GetPluginPath(fileName);

            if (!File.Exists(filePath))
            {
                UDebug.Log("找不到");
               
                Save(defaultValue, fileName);
                return defaultValue;
            }

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                UDebug.Log("内容为空");
                Save(defaultValue, fileName);
                return defaultValue;
            }
            return JsonConvert.DeserializeObject<T>(json);
            
        }
        private static string GetPluginPath(string fileName)
        {           
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";
            string dllDir = Path.GetDirectoryName(typeof(JsonHelper).Assembly.Location) ?? Paths.PluginPath;
            return Path.Combine(dllDir, fileName);
        }
    }
}
