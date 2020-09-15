using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vostok.Commons.Local.Helpers
{
    public class JavaProperties
    {
        private readonly Dictionary<string, string> properties;

        public JavaProperties(Dictionary<string, string> properties)
        {
            this.properties = properties;
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, BuildFile());
        }

        private string BuildFile()
        {
            return string.Join("\n", properties.Select(kv => $"{kv.Key}={EscapeValue(kv.Value)}"));
        }

        private string EscapeValue(string value)
        {
            return value.Replace("\\", "\\\\");
        }
    }
}