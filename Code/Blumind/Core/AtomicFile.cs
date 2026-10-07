using System;
using System.IO;

namespace Blumind.Core
{
    static class AtomicFile
    {
        // The temp file is on the destination volume. Never truncate the existing file.
        public static void Write(string filename, Action<Stream> write)
        {
            string target = Path.GetFullPath(filename);
            string temporary = Path.Combine(Path.GetDirectoryName(target),
                "." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    write(output);
                    output.Flush(true);
                }

                if (File.Exists(target))
                    File.Replace(temporary, target, target + ".bak");
                else
                    File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }
}
