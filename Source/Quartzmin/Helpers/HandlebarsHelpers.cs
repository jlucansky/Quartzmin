using static Quartzmin.Controllers.PageControllerBase;

namespace Quartzmin.Helpers;

internal class HandlebarsHelpers
{
    private readonly Services _services;

    public HandlebarsHelpers(Services services)
    {
        _services = services;
    }

    public static void Register(Services services)
    {
        new HandlebarsHelpers(services).RegisterInternal();
    }

    private void RegisterInternal()
    {
        IHandlebars h = _services.Handlebars;

        PartialViewHelper.RegisterPartialView(h);

        h.RegisterHelper("Upper",
            (o, c, a) => o.Write(a[0].ToString()?.ToUpper()));
        h.RegisterHelper("Lower",
            (o, c, a) => o.Write(a[0].ToString()?.ToLower()));
        h.RegisterHelper("LocalTimeZoneInfoId",
            (o, c, a) => o.Write(TimeZoneInfo.Local.Id));
        h.RegisterHelper("SystemTimeZonesJson",
            (o, c, a) => Json(o, c, a, TimeZoneInfo.GetSystemTimeZones().ToDictionary()));
        h.RegisterHelper("DefaultDateFormat",
            (o, c, a) => o.Write(DateTimeSettings.DefaultDateFormat));
        h.RegisterHelper("DefaultTimeFormat",
            (o, c, a) => o.Write(DateTimeSettings.DefaultTimeFormat));
        h.RegisterHelper("SerializeTypeHandler",
            (o, c, a) => o.WriteSafeString(_services.TypeHandlers.Serialize((TypeHandlerBase)c.Value)));
        h.RegisterHelper("Disabled",
            (o, c, a) => { if (IsTrue(a[0])) o.Write("disabled"); });
        h.RegisterHelper("Checked",
            (o, c, a) => { if (IsTrue(a[0])) o.Write("checked"); });
        h.RegisterHelper("nvl",
            (o, c, a) => o.Write(a[a[0] == null ? 1 : 0]));
        h.RegisterHelper("not", (o, c, a) => o.Write(IsTrue(a[0]) ? "False" : "True"));

        h.RegisterHelper(nameof(BaseUrl),
            (o, c, a) => WriteBaseUrl(o, c, a));
        h.RegisterHelper(nameof(MenuItemActionLink),
            (o, c, a) => MenuItemActionLink(o, c, a));
        h.RegisterHelper(nameof(RenderJobDataMapValue),
            (o, c, a) => RenderJobDataMapValue(o, c, a));
        h.RegisterHelper(nameof(ViewBag),
            (o, c, a) => ViewBag(o, c, a));
        h.RegisterHelper(nameof(ActionUrl),
            (o, c, a) => ActionUrl(o, c, a));
        h.RegisterHelper(nameof(Json),
            (o, c, a) => Json(o, c, a));
        h.RegisterHelper(nameof(Selected),
            (o, c, a) => Selected(o, c, a));
        h.RegisterHelper(nameof(IsType),
            (o, opt, c, a) => IsType(o, opt, c, a));
        h.RegisterHelper(nameof(EachPair),
            (o, opt, c, a) => EachPair(o, opt, c, a));
        h.RegisterHelper(nameof(EachItems),
            (o, opt, c, a) => EachItems(o, opt, c, a));
        h.RegisterHelper(nameof(ToBase64),
            (o, c, a) => ToBase64(o, c, a));
        h.RegisterHelper(nameof(Footer),
            (o, opt, c, a) => Footer(o, opt, c, a));
        h.RegisterHelper(nameof(QuartzminVersion),
            (o, c, a) => QuartzminVersion(o, c, a));
        h.RegisterHelper(nameof(Logo),
            (o, c, a) => Logo(o, c, a));
        h.RegisterHelper(nameof(ProductName),
            (o, c, a) => ProductName(o, c, a));
    }

    static bool IsTrue(object value) => value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    string HtmlEncode(object value) => _services.ViewEngine.Encode(value);

    string UrlEncode(string value) => HttpUtility.UrlEncode(value);

    private string BaseUrl
    {
        get
        {
            return $"{_services.Options.VirtualPathRoot}";
        }
    }

    private void WriteBaseUrl(EncodedTextWriter output, Context context, Arguments arguments)
    {
        output.WriteSafeString($"{BaseUrl}");
    }

    private string AddQueryString(string uri, IEnumerable<KeyValuePair<string, object>> queryString)
    {
        if (queryString == null)
        {
            return uri;
        }

        var anchorIndex = uri.IndexOf('#');
        var uriToBeAppended = uri;
        var anchorText = "";

        // If there is an anchor, then the query string must be inserted before its first occurence.
        if (anchorIndex != -1)
        {
            anchorText = uri.Substring(anchorIndex);
            uriToBeAppended = uri.Substring(0, anchorIndex);
        }

        var queryIndex = uriToBeAppended.IndexOf('?');
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder();
        sb.Append(uriToBeAppended);

        foreach (var parameter in queryString)
        {
            sb.Append(hasQuery ? '&' : '?');
            sb.Append(UrlEncode(parameter.Key));
            sb.Append('=');
            sb.Append(UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}", parameter.Value)));
            hasQuery = true;
        }

        sb.Append(anchorText);
        return sb.ToString();
    }

    private void ViewBag(EncodedTextWriter output, Context context, Arguments arguments)
    {
        var dict = (IDictionary<string, object>)arguments[0];
        var viewBag = (IDictionary<string, object>)context["ViewBag"];

        foreach (var pair in dict)
        {
            viewBag[pair.Key] = pair.Value;
        }
    }

    private void MenuItemActionLink(EncodedTextWriter output, Context context, Arguments arguments)
    {
        var dict = arguments[0] as IDictionary<string, object> ?? new Dictionary<string, object>() { ["controller"] = arguments[0] };

        string classes = "item";
        if (dict["controller"].Equals(context.GetValue<string>("ControllerName")))
        {
            classes += " active";
        }

        string url = BaseUrl + dict["controller"];
        string title = HtmlEncode(dict.GetValue("title", dict["controller"]));

        output.WriteSafeString($@"<a href=""{url}"" class=""{classes}"">{title}</a>");
    }

    private void ActionUrl(EncodedTextWriter output, Context context, Arguments arguments)
    {
        if (arguments.Length is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(arguments));
        }

        IDictionary<string, object> routeValues = null;
        string controller = null;
        string action = (arguments[0] as Page)?.ActionName ?? (string)arguments[0];

        if (arguments.Length >= 2) // [actionName, controllerName/routeValues ]
        {
            switch (arguments[1])
            {
                case IDictionary<string, object> r:
                    routeValues = r;
                    break;
                case string s:
                    controller = s;
                    break;
                case Page v:
                    controller = v.ControllerName;
                    break;
                default:
                    throw new InvalidDataException("ActionUrl: Invalid parameter 1");
            }
        }

        if (arguments.Length == 3) // [actionName, controllerName, routeValues]
        {
            routeValues = (IDictionary<string, object>)arguments[2];
        }

        controller ??= context.GetValue<string>("ControllerName");

        string url = BaseUrl + controller;

        if (!string.IsNullOrEmpty(action))
        {
            url += "/" + action;
        }

        output.WriteSafeString(AddQueryString(url, routeValues));
    }

    private void Selected(EncodedTextWriter output, Context context, Arguments arguments)
    {
        string selected = arguments.Length >= 2 ? arguments[1]?.ToString() : context["selected"].ToString();

        if (((string)arguments[0]).Equals(selected, StringComparison.InvariantCultureIgnoreCase))
        {
            output.Write("selected");
        }
    }

    private void Json(EncodedTextWriter output, Context context, Arguments arguments, params object[] args)
    {
        if (arguments.Length > 0)
        {
            output.WriteSafeString(JsonConvert.SerializeObject(arguments[0]));
        }

        if (args.Length <= 0)
        {
            return;
        }

        output.WriteSafeString(JsonConvert.SerializeObject(args[0]));
    }

    private void RenderJobDataMapValue(EncodedTextWriter output, Context context, Arguments arguments)
    {
        var item = (JobDataMapItem)arguments[0];
        output.WriteSafeString(item.SelectedType.RenderView(_services, item.Value));
    }

    private void IsType(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
    {
        Type[] expectedType;

        var strType = (string)arguments[1];

        expectedType = strType switch
        {
            "IEnumerable<string>" => new[] { typeof(IEnumerable<string>) },
            "IEnumerable<KeyValuePair<string, string>>" => new[] { typeof(IEnumerable<KeyValuePair<string, string>>) },
            _ => throw new ArgumentException("Invalid type: " + strType)
        };

        var t = arguments[0]?.GetType();

        if (expectedType.Any(x => x.IsAssignableFrom(t)))
        {
            options.Template(writer, context.Value);
        }
        else
        {
            options.Inverse(writer, context.Value);
        }
    }

    private void EachPair(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
    {
        void OutputElements<T>()
        {
            if (arguments[0] is not IEnumerable<T> pairs)
            {
                return;
            }

            foreach (var item in pairs)
            {
                options.Template(writer, item);
            }
        }

        OutputElements<KeyValuePair<string, string>>();
        OutputElements<KeyValuePair<string, object>>();
    }

    private void EachItems(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
    {
        EachPair(writer, options, context, ((dynamic)arguments[0]).GetItems());
    }

    private void ToBase64(EncodedTextWriter output, Context context, Arguments arguments)
    {
        var bytes = (byte[])arguments[0];

        if (bytes != null)
        {
            output.Write(Convert.ToBase64String(bytes));
        }
    }

    private void Footer(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
    {
        var viewBag = (IDictionary<string, object>)context["ViewBag"];

        if (viewBag.TryGetValue("ShowFooter", out var show) && (bool)show)
        {
            options.Template(writer, context.Value);
        }
    }

    private void QuartzminVersion(EncodedTextWriter output, Context context, Arguments arguments)
    {
        var v = GetType().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault();
        output.Write(v.InformationalVersion);
    }

    private void Logo(EncodedTextWriter output, Context context, Arguments arguments)
    {
        output.Write(_services.Options.Logo);
    }

    private void ProductName(EncodedTextWriter output, Context context, Arguments arguments)
    {
        output.Write(_services.Options.ProductName);
    }
}