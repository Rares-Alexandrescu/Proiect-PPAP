using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers
{
    public static class SecurityHelper
    {
        public static string CripteazaParola(string parola)
        {
            return BCrypt.Net.BCrypt.HashPassword(parola);
        }

        public static bool VerificaParola(string stringIntrodus, string stringCriptat)
        {
            return BCrypt.Net.BCrypt.Verify(stringIntrodus, stringCriptat);
        }

        public static string CripteazaCNP(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerificaCnp(string cnpIntrodus, string cnpCriptatInDb)
        {
            return CripteazaCNP(cnpIntrodus) == cnpCriptatInDb;
        }
    }
}