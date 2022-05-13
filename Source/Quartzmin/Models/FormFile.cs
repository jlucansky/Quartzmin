using System.IO;

namespace Quartzmin.Models
{
    public class FormFile
    {
        private readonly Microsoft.AspNetCore.Http.IFormFile _file;
        public FormFile(Microsoft.AspNetCore.Http.IFormFile file) => _file = file;

        public Stream GetStream() => _file.OpenReadStream();

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
