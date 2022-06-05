namespace Quartzmin.Controllers;

public class AuthController : PageControllerBase
{
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
    public async Task<IActionResult> LoginAsync([FromBody]LoginModel model)
    {
        var dataBytes = Convert.FromBase64String(model.Data);
        var modelData = JsonConvert.DeserializeObject<LoginViewModel>(Encoding.ASCII.GetString(dataBytes));

        if (modelData is null)
        {
            return BadRequest("Failed!");
        }

        var validUser = ValidateUser(modelData);
        if (validUser == null)
        {
            return BadRequest("Failed!");
        }

        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, validUser.UserName), new (ClaimTypes.Role, validUser.Role)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            RedirectUri = null,
            IssuedUtc = null,
            ExpiresUtc = null,
            AllowRefresh = null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok(new { returnUrl = $"{Services.Options.VirtualPathRoot}/Scheduler" });
    }

    [HttpPost]
    public async Task<IActionResult> LogoutAsync()
    {
        // Clear the existing external cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    private SystemUser ValidateUser(LoginViewModel user)
    {
        var content = System.IO.File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json"));
        var users = JsonConvert.DeserializeObject<List<SystemUser>>(content);

        return users?.FirstOrDefault(u =>
            u.UserName.Equals(user.Username, StringComparison.InvariantCultureIgnoreCase)
            && u.Password.Equals(user.Password.HashSHA256()));
    }
}