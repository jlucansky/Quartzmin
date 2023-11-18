using Microsoft.AspNetCore.Builder;
using Quartzmin;

const string virtialPathRoot = "/quartzmin";

var builder = WebApplication.CreateBuilder();

builder.Services.AddQuartzmin(virtialPathRoot, "test");

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Items["header"] = "a";
    await next.Invoke(context);
});

app.UseQuartzmin(new QuartzminOptions
{
    Scheduler = DemoScheduler.Create().Result
});

app.UseRouting();
app.MapGet("/", () => "Hello");

app.Run();