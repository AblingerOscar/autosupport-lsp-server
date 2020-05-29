using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
        }

        public static string ReadFile(string relativePath)
        {
            var resourceName = GetEmbeddedResourcePathOf(relativePath);

            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new NullReferenceException($"Could not read resource '{resourceName}': It probably doesn't exist");
            }

            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private static string GetEmbeddedResourcePathOf(string relativePath)
        {
            return "Tests." + relativePath;
        }
    }
}
