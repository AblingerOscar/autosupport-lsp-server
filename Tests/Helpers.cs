using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tests
{
    public static class Helpers
    {

        public static string GetAbsolutePathOf(string relativePath)
        {
            string[] resources = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames();

            string resourceName = resources
                .Single(str => str.EndsWith("YourFileName.txt"));

            return resourceName;

            //return relativePath;

            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return Path.Combine(dirPath, relativePath);
        }

        public static string ReadFile(string relativePath)
        {
            var resourceName = GetEmbeddedResourcePathOf(relativePath);

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static string GetEmbeddedResourcePathOf(string relativePath)
        {
            return "Tests." + relativePath;
        }
    }
}
