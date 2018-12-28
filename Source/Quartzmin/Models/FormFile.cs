using System.IO;

namespace Quartzmin.Models
{
    public class FormFile
    {
#if NETSTANDARD
        readonly Microsoft.AspNetCore.Http.IFormFile _file;
        public FormFile(Microsoft.AspNetCore.Http.IFormFile file) => _file = file;

        public Stream GetStream() => _file.OpenReadStream();
#endif
#if NETFRAMEWORK
        readonly System.Net.Http.HttpContent _content;
        public FormFile(System.Net.Http.HttpContent content) => _content = content;

        public Stream GetStream() => _content.ReadAsStreamAsync().GetAwaiter().GetResult();
#endif

        public byte[] GetBytes()
        {
            using (var stream = new MemoryStream())
            {
                GetStream().CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}
