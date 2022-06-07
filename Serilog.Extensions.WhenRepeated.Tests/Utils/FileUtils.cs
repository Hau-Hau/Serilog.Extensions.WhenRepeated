using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Serilog.Extensions.WhenRepeated.Tests.Utils
{
    public static class FileUtils
    {
        public static async ValueTask<string[]> ReadAllLinesSafeAsync(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var file = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        continue;
                    }

                    file.Add(line);
                }

                return file.ToArray();
            }
        }
    }
}
