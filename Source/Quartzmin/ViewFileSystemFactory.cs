namespace Quartzmin;

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

    private sealed class DiskFileSystem : ViewEngineFileSystem
    {
        private readonly string _root;

        public DiskFileSystem(string root)
        {
            _root = root;
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

        private string GetFullPath(string filePath)
        {
            return Path.Combine(_root, filePath.Replace("partials/", "Partials/").Replace('/', Path.DirectorySeparatorChar));
        }
    }

    private sealed class EmbeddedFileSystem : ViewEngineFileSystem
    {
        public override string GetFileContent(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetFullPath(filename));
            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        protected override string CombinePath(string dir, string otherFileName)
        {
            return Path.Combine(dir, otherFileName);
        }

        public override bool FileExists(string filePath)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceInfo(GetFullPath(filePath)) != null;
        }

        private string GetFullPath(string filePath)
        {
            return Path.Combine(nameof(Quartzmin) + ".Views", filePath.Replace("partials/", "Partials/")).Replace('/', '.').Replace('\\', '.');
        }
    }
}