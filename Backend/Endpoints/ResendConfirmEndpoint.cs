using Dapper;
using System.Data;
using System.Security.Claims;
using Backend.Helpers;
using Backend.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Backend.Endpoints
{
    public static class ResendConfirmEndpoint
    {
        public static void MapResendConfirmareEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost("/resend-confirmare", async Task<IResult>(ClaimsPrincipal user, 
                IConfiguration config,
                IDataProtectionProvider dataProtector,
                IEmailService emailService) =>
            {
                var idString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var NumeUtilizator = user.FindFirst(ClaimTypes.Name)?.Value;
                var EmailUtilizator = user.FindFirst(ClaimTypes.Email)?.Value;

                var PrenumeUtilizator = user.FindFirst("Prenume")?.Value;

                Console.WriteLine("Am gasit id-ul pentru retrimiterea confirmarii " + idString);

                if (!int.TryParse(idString, out int idUtilizatorLogat)) return Results.Unauthorized();

                if (string.IsNullOrEmpty(EmailUtilizator))
                {
                    return Results.BadRequest(new { mesaj = "Token-ul nu conține o adresă de email validă." });
                }

                try
                {
                    Console.WriteLine("1");
                    var protector = dataProtector.CreateProtector("VerificareCont").ToTimeLimitedDataProtector();
                    Console.WriteLine("2");
                    string tokenSecurizat = protector.Protect(idUtilizatorLogat.ToString(), TimeSpan.FromHours(24));
                    Console.WriteLine("3");
                    await emailService.TrimiteEmailConfirmAsync(EmailUtilizator, NumeUtilizator, PrenumeUtilizator, tokenSecurizat);
                    return Results.Ok(new { mesaj = "Email-ul a fost retrimis cu succes!" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare la trimiterea emailului: {ex.Message}");
                    return Results.Problem("Eroare la procesarea cererii de email. Te rugăm să încerci mai târziu.");
                }


            }).RequireAuthorization();
        }
    }

}