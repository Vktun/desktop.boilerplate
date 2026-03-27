using System;

namespace Dabp.Utils.Security
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);

        bool VerifyPassword(string password, string hashedPassword);
    }
}
