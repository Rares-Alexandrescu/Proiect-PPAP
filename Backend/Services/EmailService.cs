using System.Net;
using System.Net.Mail;

namespace Backend.Services
{
    public interface IEmailService
    {
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
            string frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost:4200";
            string anulCurent = DateTime.Now.Year.ToString();
            Console.WriteLine("Nu ajungem dupa caleTemplate");
            string caleTemplate = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "WelcomeEmail.html");
            Console.WriteLine("PATH: " + caleTemplate);
            Console.WriteLine("EXISTS: " + File.Exists(caleTemplate));
            string htmlBrut = await File.ReadAllTextAsync(caleTemplate);

            Console.WriteLine("PATH TEMPLATE EMAIL:");
            Console.WriteLine(caleTemplate);
            Console.WriteLine("EXISTS: " + File.Exists(caleTemplate));

            string htmlPersonalizat = htmlBrut
                .Replace("{{Nume}}", nume)
                .Replace("{{Prenume}}", prenume)
                .Replace("{{LinkConfirmare}}", frontendUrl)
                .Replace("{{AnulCurent}}", anulCurent);
            
            await TrimiteEmailBazaAsync(emailDestinatar, "Bine ai venit! Cont creat cu succes", htmlPersonalizat);
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