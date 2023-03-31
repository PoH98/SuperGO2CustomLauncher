using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GalaxyOrbit4Launcher
{
    internal static class Helper
    {
        public static IEnumerable<string> GetAllFiles(string path, string searchPattern = "*.*")
        {
            return Directory.EnumerateFiles(path, searchPattern).Union(
                Directory.EnumerateDirectories(path).SelectMany(d =>
                {
                    try
                    {
                        return GetAllFiles(d, searchPattern);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return Enumerable.Empty<string>();
                    }
                }));
        }
    }
}
