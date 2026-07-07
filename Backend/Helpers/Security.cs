using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens; 
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;
using System.Text;
using Backend.DBClasses;
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
                return builder.ToString().Substring(0, 13);
            }
        }

        public static bool VerificaCnp(string cnpIntrodus, string cnpCriptatInDb)
        {
            return CripteazaCNP(cnpIntrodus) == cnpCriptatInDb;
        }

        public static string CreareJWTLogin(Utilizator utilizator, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var cheieSecreta = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                            new Claim(ClaimTypes.NameIdentifier, utilizator.Id.ToString()),
                            new Claim(ClaimTypes.Email, utilizator.Email),
                            new Claim(ClaimTypes.Name, utilizator.Nume)
                        }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(cheieSecreta),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}