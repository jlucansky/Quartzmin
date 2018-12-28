<p align="center">
    <img src="https://raw.githubusercontent.com/jlucansky/public-assets/master/Quartzmin/logo.png" height="150">
</p>

## Introduction
Quartzmin is powerful, easy to use web management tool for Quartz.NET

Quartzmin can be used within your existing application with minimum configuration effort as a Quartz.NET plugin when it automatically creates embedded web server. Or it can be plugged into your existing OWIN-based web application as a middleware.

> [Quartz.NET](https://www.quartz-scheduler.net) is a full-featured, open source job scheduling system that can be used from smallest apps to large scale enterprise systems.

![Demo](https://raw.githubusercontent.com/jlucansky/public-assets/master/Quartzmin/demo.gif)

The goal of this project is to provide convenient tool to utilize most of the functionality that Quartz.NET enables. The biggest challenge was to create a simple yet effective editor of job data map which is heart of Quartz.NET. Every job data map item is strongly typed and Quartzmin can be easily extended with a custom editor for your specific type beside standard supported types such as String, Integer, DateTime and so on. 

Quartzmin was created with **Semantic UI** and **Handlebars.Net** as the template engine.
