#if NETFRAMEWORK

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MultipartDataMediaFormatter.Converters;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Infrastructure.Logger;

namespace MultipartDataMediaFormatter
{
    public class FormMultipartEncodedMediaTypeFormatter : MediaTypeFormatter
    {
        private const string SupportedMediaType = "multipart/form-data";
        
        private readonly MultipartFormatterSettings Settings;

        public FormMultipartEncodedMediaTypeFormatter(MultipartFormatterSettings settings = null)
        {
            Settings = settings ?? new MultipartFormatterSettings();
            SupportedMediaTypes.Add(new MediaTypeHeaderValue(SupportedMediaType));
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);

            //need add boundary
            //(if add when fill SupportedMediaTypes collection in class constructor then receive post with another boundary will not work - Unsupported Media Type exception will thrown)
            if (headers.ContentType == null)
            {
                headers.ContentType = new MediaTypeHeaderValue(SupportedMediaType);
            }
            if (!String.Equals(headers.ContentType.MediaType, SupportedMediaType, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Not a Multipart Content");
            }
            if (headers.ContentType.Parameters.All(m => m.Name != "boundary"))
            {
                headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "MultipartDataMediaFormatterBoundary1q2w3e"));
            }
        }

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content,
                                                               IFormatterLogger formatterLogger)
        {
            var httpContentToFormDataConverter = new HttpContentToFormDataConverter();
            FormData multipartFormData = await httpContentToFormDataConverter.Convert(content);

            readStream.Position = 0;

            IFormDataConverterLogger logger;
            if (formatterLogger != null)
                logger = new FormatterLoggerAdapter(formatterLogger);
            else 
                logger = new FormDataConverterLogger();

            var dataToObjectConverter = new FormDataToObjectConverter(multipartFormData, logger, Settings);
            object result = dataToObjectConverter.Convert(type);

            logger.EnsureNoErrors();

            return result;
        }

        public override async Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
                                                TransportContext transportContext)
        {
            if (!content.IsMimeMultipartContent())
            {
                throw new Exception("Not a Multipart Content");
            }

            var boudaryParameter = content.Headers.ContentType.Parameters.FirstOrDefault(m => m.Name == "boundary" && !String.IsNullOrWhiteSpace(m.Value));
            if (boudaryParameter == null)
            {
                throw new Exception("multipart boundary not found");
            }

            var objectToMultipartDataByteArrayConverter = new ObjectToMultipartDataByteArrayConverter(Settings);
            byte[] multipartData = objectToMultipartDataByteArrayConverter.Convert(value, boudaryParameter.Value);

            await writeStream.WriteAsync(multipartData, 0, multipartData.Length);

            content.Headers.ContentLength = multipartData.Length;
        }
    }
}
#endif