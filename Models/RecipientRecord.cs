using System;
using System.Collections.Generic;

namespace CetakSertifikatApp.Models
{
    public class RecipientRecord
    {
        public int RowIndex { get; set; }
        public Dictionary<string, string> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string GetValue(string key, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;
            if (Data.TryGetValue(key.Trim(), out var val))
            {
                return val ?? defaultValue;
            }
            return defaultValue;
        }

        public string ReplacePlaceholders(string template)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            string result = template;
            foreach (var kvp in Data)
            {
                // Replace case-insensitive {key}
                string placeholder = "{" + kvp.Key + "}";
                result = ReplaceInsensitive(result, placeholder, kvp.Value ?? string.Empty);
            }
            return result;
        }

        private static string ReplaceInsensitive(string str, string oldValue, string newValue)
        {
            int index = 0;
            while ((index = str.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                str = str.Remove(index, oldValue.Length).Insert(index, newValue);
                index += newValue.Length;
            }
            return str;
        }
    }
}
