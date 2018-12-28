using System;
using System.Collections.Generic;
using System.Globalization;

namespace Quartzmin
{
    public class ViewEngine
    {
        readonly Services _services;
        readonly Dictionary<string, Func<object, string>> _compiledViews = new Dictionary<string, Func<object, string>>(StringComparer.OrdinalIgnoreCase);

        public bool UseCache { get; set; }

        public ViewEngine(Services services)
        {
            _services = services;
            UseCache = string.IsNullOrEmpty(services.Options.ViewsRootDirectory);
        }

        Func<object, string> GetRenderDelegate(string templatePath)
        {
            if (UseCache)
            {
                lock (_compiledViews)
                {
                    if (!_compiledViews.ContainsKey(templatePath))
                    {
                        _compiledViews[templatePath] = _services.Handlebars.CompileView(templatePath);
                    }

                    return _compiledViews[templatePath];
                }
            }
            else
            {
                return _services.Handlebars.CompileView(templatePath);
            }
        }

        public string Render(string templatePath, object model)
        {
            return GetRenderDelegate(templatePath)(model);
        }

        public string Encode(object value)
        {
            return _services.Handlebars.Configuration.TextEncoder.Encode(string.Format(CultureInfo.InvariantCulture, "{0}", value));
        }

        public string ErrorPage(Exception ex)
        {
            return Render("Error.hbs", new
            {
                ex.GetBaseException().GetType().FullName,
                Exception = ex,
                BaseException = ex.GetBaseException(),
                Dump = ex.ToString()
            });
        }
    }
}
