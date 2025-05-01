using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace DELTAAPI.Helpers
{
    public static class SecurityHelper
    {
        public static byte[] GenerateSalt(int size = 16)
        {
            var salt = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return salt;
        }

        public static byte[] ComputeHash(string password, byte[] salt)
        {
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
            return sha256.ComputeHash(combined);
        }

        public static byte[] GeneratePasswordHash(string password, out byte[] salt)
        {
            salt = GenerateSalt();
            return ComputeHash(password, salt);
        }

        public static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            var computedHash = ComputeHash(password, storedSalt);
            return storedHash.SequenceEqual(computedHash);
        }
    }
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                  .FirstOrDefault() as DisplayAttribute;

            return attribute?.Name ?? value.ToString();
        }
    }

}
