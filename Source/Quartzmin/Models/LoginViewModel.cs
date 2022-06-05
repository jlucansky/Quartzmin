namespace Quartzmin.Models;

public class LoginViewModel
{
    public string Username { get; set; }

    public string Password { get; set; }

    public string ReturnUrl { get; set; }
}

public class LoginModel
{
    public string Data { get; set; }
}

// Default: admin.1 : P@s$w0rD!12e456s
public class SystemUser
{
    public string UserName { get; set; }

    public string Password { get; set; }

    public string Role { get; set; }
}