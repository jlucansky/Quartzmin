namespace Quartzmin;

public class ViewEngine
{
    private readonly Services _services;
    private readonly Dictionary<string, HandlebarsTemplate<object, string>> _compiledViews = new (StringComparer.OrdinalIgnoreCase);

    private readonly bool _useCache;

    public ViewEngine(Services services)
    {
        _services = services;
        _useCache = string.IsNullOrEmpty(services.Options.ViewsRootDirectory);
    }

    HandlebarsTemplate<object, string> GetRenderDelegate(string templatePath)
    {
        if (!_useCache)
        {
            return _services.Handlebars.CompileView(templatePath);
        }

        lock (_compiledViews)
        {
            if (!_compiledViews.ContainsKey(templatePath))
            {
                _compiledViews[templatePath] = _services.Handlebars.CompileView(templatePath);
            }

            return _compiledViews[templatePath];
        }
    }

    public string Render(string templatePath, object model)
    {
        return GetRenderDelegate(templatePath)(model);
    }

    public string Encode(object value)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}", value);
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