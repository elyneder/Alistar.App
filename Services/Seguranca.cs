using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alistar.App.Services
{
    public static class Security
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

    }
}
