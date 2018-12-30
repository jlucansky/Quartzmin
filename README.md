<p align="center">
    <img src="https://raw.githubusercontent.com/jlucansky/public-assets/master/Quartzmin/logo.png" height="150">
</p>

---

[![NuGet](https://img.shields.io/nuget/v/Quartzmin.svg)](https://www.nuget.org/packages/Quartzmin)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Quartzmin is powerful, easy to use web management tool for Quartz.NET

Quartzmin can be used within your existing application with minimum effort as a Quartz.NET plugin when it automatically creates embedded web server. Or it can be plugged into your existing OWIN-based web application as a middleware.

> [Quartz.NET](https://www.quartz-scheduler.net) is a full-featured, open source job scheduling system that can be used from smallest apps to large scale enterprise systems.

![Demo](https://raw.githubusercontent.com/jlucansky/public-assets/master/Quartzmin/demo.gif)

The goal of this project is to provide convenient tool to utilize most of the functionality that Quartz.NET enables. The biggest challenge was to create a simple yet effective editor of job data map which is heart of Quartz.NET. Every job data map item is strongly typed and Quartzmin can be easily extended with a custom editor for your specific type beside standard supported types such as String, Integer, DateTime and so on. 

Quartzmin was created with **Semantic UI** and **Handlebars.Net** as the template engine.

## Features
- Add, modify jobs and triggers
- Add, modify calenars (annual, cron, daily, holiday, monthly, weekly)
- Change trigger type to Cron, Simple, Calendar Interval or Daily Time Interval
- Set typed job data map values (bool, DateTime, int, float, long, double, decimal, string, byte[])
- Create custom type editor for complex type in job data map
- Manage scheduler state (standby, shutdown)
- Pause and resume job and trigger groups
- Pause and resume triggers individually
- Pause and resume all triggers for specific job
- Trigger specific job immediately
- Watch currently executing jobs
- Interrupt executing job
- See next scheduled dates for Cron
- See recent job history, state and error messages

## Install
Quartzmin is available on [nuget.org](https://www.nuget.org/packages/Quartzmin)

To install Quartzmin, run the following command in the Package Manager Console
```powershell
PM> Install-Package Quartzmin
```
## Minimum requirements
- .NET Framework 4.5.2 
- .NET Standard 2.0

## Usage
### Embedded web server
Everything you should do is just install [Quartzmin.SelfHost](https://www.nuget.org/packages/Quartzmin.SelfHost) package and configure `QuartzminPlugin` and `ExecutionHistoryPlugin` to support histograms and statistics.

Run the following command in the Package Manager Console:
```powershell
PM> Install-Package Quartzmin.SelfHost
```
Add to your `App.config` file:
```xml
<configuration>
  <configSections>
    <section name="quartz" type="System.Configuration.NameValueFileSectionHandler" />
  </configSections>

  <quartz>
    <add key="quartz.plugin.quartzmin.type" value="Quartzmin.SelfHost.QuartzminPlugin, Quartzmin.SelfHost" />
    <add key="quartz.plugin.quartzmin.url" value="http://localhost:5000" />
      
    <add key="quartz.plugin.recentHistory.type" value="Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin, Quartz.Plugins.RecentHistory" />
    <add key="quartz.plugin.recentHistory.storeType" value="Quartz.Plugins.RecentHistory.Impl.InProcExecutionHistoryStore, Quartz.Plugins.RecentHistory" />
  </quartz>
</configuration>
```
Start Quartz.NET scheduler somewhere:
```csharp
StdSchedulerFactory.GetDefaultScheduler().Result.Start();
```

### OWIN middleware
Add to your `Startup.cs` file:
```csharp
public void Configuration(IAppBuilder app)
{
    app.UseQuartzmin(new QuartzminOptions()
    {
        Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result
    });
}
```

### ASP.NET Core middleware
Add to your `Startup.cs` file:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddQuartzmin();
}

public void Configure(IApplicationBuilder app)
{
    app.UseQuartzmin(new QuartzminOptions()
    {
        Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result
    });
}
```

## Notes
In clustered environment, it make more sense to host Quarzmin on single dedicated Quartz.NET node in standby mode and implement own `IExecutionHistoryStore` depending on database or ORM framework you typically incorporate. Every clustered Quarz.NET node should be configured with `ExecutionHistoryPlugin` and only dedicated node for management may have `QuartzminPlugin`.


## License
This project is made available under the MIT license. See [LICENSE](LICENSE) for details.
