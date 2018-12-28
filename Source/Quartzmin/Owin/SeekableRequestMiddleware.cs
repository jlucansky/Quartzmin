#if NETFRAMEWORK

using Microsoft.Owin;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Management;

namespace Quartzmin.Owin
{
    internal class SeekableRequestMiddleware : OwinMiddleware
    {
        public SeekableRequestMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            // read out body (wait for all bytes)
            using (var streamCopy = new MemoryStream())
            {
                try
                {
                    context.Request.Body.CopyTo(streamCopy);
                }
                catch (HttpException ex) when (ex?.WebEventCode == WebEventCodes.RuntimeErrorPostTooLarge)
                {
                    context.Response.Body = null;
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    return;   
                }

                streamCopy.Position = 0; // rewind
                context.Request.Body = streamCopy; // put back in place for downstream handlers

                await Next.Invoke(context);
            }
        }
    }
}

#endif