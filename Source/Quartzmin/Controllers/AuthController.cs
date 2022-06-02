using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quartzmin.Models;

namespace Quartzmin.Controllers;

public class AuthController : PageControllerBase
{
    public AuthController()
    {
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string returnUrl)
    {
        return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult Login([FromBody]LoginModel model)
    {
        Console.WriteLine(JsonConvert.SerializeObject(model));
        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        return View(new
        {
            a = "a"
        });
    }
}