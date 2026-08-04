using UsersService.Application;
using System.Security.Cryptography;
using System.Text;

namespace UsersService.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; //Соль решил добавить для защиты одинаковых паролей и усложнения перебора "словарем" по таблицам хэшей
    private const char Separator = ':';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return $"{Convert.ToHexString(salt)}{Separator}{Convert.ToHexString(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split(Separator, 2);

        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromHexString(parts[0]);
            var expectedHash = Convert.FromHexString(parts[1]);

            if (salt.Length != SaltSize)
            {
                return false;
            }

            var actualHash = ComputeHash(password, salt);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltedPassword = new byte[passwordBytes.Length + salt.Length];

        Buffer.BlockCopy(
            passwordBytes,
            0,
            saltedPassword,
            0,
            passwordBytes.Length);

        Buffer.BlockCopy(
            salt,
            0,
            saltedPassword,
            passwordBytes.Length,
            salt.Length); //Докидываем соль в конец при хэшировании

        return SHA256.HashData(saltedPassword);
    }
}
