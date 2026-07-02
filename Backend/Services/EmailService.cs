using System.Net;
using System.Net.Mail;

namespace Backend.Services
{
    public interface IEmailService
    {
        Task TrimiteEmailResetareParolaAsync(string emailDestinatar, string nume, string prenume, string tokenSecurizat);
        Task TrimiteEmailWelcomeAsync(string emailDestinatar, string nume, string prenume, string tokenSecurizat);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration configuration)
        {
            _config = configuration;
        }

        public async Task TrimiteEmailWelcomeAsync(string emailDestinatar, string nume, string prenume, string tokenSecurizat)
        {

            string backendUrl = _config["Backend:BaseUrl"] ?? "http://localhost:5298";
            string frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            string anulCurent = DateTime.Now.Year.ToString();
            Console.WriteLine("Nu ajungem dupa caleTemplate");
            string caleTemplate = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "WelcomeEmail.html");

            Console.WriteLine("PATH TEMPLATE EMAIL:");
            Console.WriteLine(caleTemplate);
            Console.WriteLine("EXISTS: " + File.Exists(caleTemplate));
            
            if (!File.Exists(caleTemplate))
            {
                throw new FileNotFoundException($"Template-ul de email nu a fost gasit la {caleTemplate}");
            }
            string htmlBrut = await File.ReadAllTextAsync(caleTemplate);

            string htmlPersonalizat = htmlBrut
                .Replace("{{Nume}}", nume)
                .Replace("{{Prenume}}", prenume)
                .Replace("{{LinkConfirmare}}", $"{backendUrl}/confirmare-cont?token={Uri.EscapeDataString(tokenSecurizat)}")
                .Replace("{{AnulCurent}}", anulCurent);

            Console.WriteLine("tokenul lu peste " + tokenSecurizat + " si vreau si frontend " + backendUrl);
            await TrimiteEmailBazaAsync(emailDestinatar, "Bine ai venit! Cont creat cu succes", htmlPersonalizat);
        }

        public async Task TrimiteEmailResetareParolaAsync(string emailDestinatar, string nume, string prenume, string tokenSecurizat)
        {
            string frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            string anulCurent = DateTime.Now.Year.ToString();

            string caleTemplate = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "ResetPasswordEmail.html");

            Console.WriteLine("PATH TEMPLATE EMAIL RESETARE:");
            Console.WriteLine(caleTemplate);
            Console.WriteLine("EXISTS: " + File.Exists(caleTemplate));

            if (!File.Exists(caleTemplate))
            {
                throw new FileNotFoundException($"Template-ul de email nu a fost gasit la {caleTemplate}");
            }

            string htmlBrut = await File.ReadAllTextAsync(caleTemplate);

            string linkResetare = $"{frontendUrl}/resetare-parola?token={Uri.EscapeDataString(tokenSecurizat)}";

            string htmlPersonalizat = htmlBrut
                .Replace("{{Nume}}", nume)
                .Replace("{{Prenume}}", prenume)
                .Replace("{{LinkResetare}}", linkResetare)
                .Replace("{{AnulCurent}}", anulCurent);

            Console.WriteLine("Link resetare generat: " + linkResetare);

            await TrimiteEmailBazaAsync(emailDestinatar, "Cerere de resetare a parolei", htmlPersonalizat);
        }



        private async Task TrimiteEmailBazaAsync(string emailDestinatar, string subiect, string mesajHtml)
        {
            var mailtrap = _config.GetSection("Mailtrap");
            
            using (var client = new SmtpClient(mailtrap["Host"], int.Parse(mailtrap["Port"]!)))
            {
                client.Credentials = new NetworkCredential(mailtrap["Username"], mailtrap["Password"]);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(mailtrap["FromEmail"]!, mailtrap["FromName"]!),
                    Subject = subiect,
                    Body = mesajHtml,
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(emailDestinatar);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}