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

        var validUser = await ValidateUserAsync(modelData);
        if (validUser == null)
        {
            return BadRequest("Failed! No user found!");
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

        return Ok(new { returnUrl = $"{Services.Options.VirtualPathRoot}Scheduler" });
    }

    [HttpPost]
    public async Task<IActionResult> LogoutAsync()
    {
        // Clear the existing external cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    private async Task<SystemUser> ValidateUserAsync(LoginViewModel user)
    {
        List<SystemUser> users;

        var usersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json");
        if (!System.IO.File.Exists(usersFile))
        {
            // build blank user file
            users = new List<SystemUser>();

            var defaultContent = JsonConvert.SerializeObject(users, Formatting.Indented);
            await System.IO.File.WriteAllTextAsync(usersFile, defaultContent).ConfigureAwait(false);
        }

        var content = await System.IO.File.ReadAllTextAsync(usersFile);
        users = JsonConvert.DeserializeObject<List<SystemUser>>(content);

        return users?.FirstOrDefault(u =>
            u.UserName.Equals(user.Username, StringComparison.InvariantCultureIgnoreCase)
            && u.Password.Equals(user.Password.HashSHA256()));
    }
}