using Alistar.App.Models;

namespace Alistar.App.Services;

public static class AuthService
{
    private static readonly List<UserAccount> Accounts =
    [
        new UserAccount
        {
            Name = "Administrador",
            Email = "admin@alistar.com",
            Password = "123456"
        }
    ];

    public static bool ValidateLogin(string email, string password)
    {
        return Accounts.Any(account =>
            string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase) &&
            account.Password == password);
    }

    public static bool UserExists(string email)
    {
        return Accounts.Any(account =>
            string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public static bool Register(string name, string email, string password)
    {
        if (UserExists(email))
        {
            return false;
        }

        Accounts.Add(new UserAccount
        {
            Name = name,
            Email = email,
            Password = password
        });

        return true;
    }
}
