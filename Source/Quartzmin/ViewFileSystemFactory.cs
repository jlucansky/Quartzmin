using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Text;

namespace Quartzmin
{
    public static class ViewFileSystemFactory
    {
        public static ViewEngineFileSystem Create(QuartzminOptions options)
        {
            ViewEngineFileSystem fs;

            if (string.IsNullOrEmpty(options.ViewsRootDirectory))
            {
                fs = new EmbeddedFileSystem();
            }
            else
            {
                fs = new DiskFileSystem(options.ViewsRootDirectory);
            }

            return fs;
        }

        private class DiskFileSystem : ViewEngineFileSystem
        {
            string root;

            public DiskFileSystem(string root)
            {
                this.root = root;
            }

            public override string GetFileContent(string filename)
            {
                return File.ReadAllText(GetFullPath(filename));
            }

            protected override string CombinePath(string dir, string otherFileName)
            {
                return Path.Combine(dir, otherFileName);
            }

            public override bool FileExists(string filePath)
            {
                return File.Exists(GetFullPath(filePath));
            }

            string GetFullPath(string filePath)
            {
                return Path.Combine(root, filePath.Replace("partials/", "Partials/").Replace('/', Path.DirectorySeparatorChar));
            }
        }

        private class EmbeddedFileSystem : ViewEngineFileSystem
        {
            public override string GetFileContent(string filename)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetFullPath(filename)))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            protected override string CombinePath(string dir, string otherFileName)
            {
                return Path.Combine(dir, otherFileName);
            }

            public override bool FileExists(string filePath)
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceInfo(GetFullPath(filePath)) != null;
            }

            string GetFullPath(string filePath)
            {
                return Path.Combine(nameof(Quartzmin) + ".Views", filePath.Replace("partials/", "Partials/")).Replace('/', '.').Replace('\\', '.');
            }
        }

    }
}
