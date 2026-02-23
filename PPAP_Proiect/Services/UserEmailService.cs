namespace PPAP_Proiect.Services
{
	public class UserEmailService
	{
		private readonly EmailSender _emailSender;
		private readonly EmailTemplateService _templateService;

		public UserEmailService(EmailSender emailSender,  EmailTemplateService templateService)
		{
			_emailSender = emailSender;
			_templateService = templateService;
		}

		public async Task TrimiteResetaerParola(string numeUser, string emailUser, string linkResetare)
		{
			var template = _templateService.GetTemplate("User_ResetareParola");

			template = template.Replace("{{Nume}}", numeUser);
			template = template.Replace("{{linkResetare}}", linkResetare);

			await _emailSender.SendEmailAsync(numeUser, emailUser, "Resetare Parola PPAP!", template);
		}
		public async Task TrimitereConfirmareCont(string numeUser, string emailUser, string linkConfirmare)
		{
			var template = _templateService.GetTemplate("User_ConfirmareCont");

			template = template.Replace("{{Nume}}", numeUser);
			template = template.Replace("{{linkConfirmare}}", linkConfirmare);

			await _emailSender.SendEmailAsync(numeUser, emailUser, "Confirmare cont PPAP!", template);
		}
	}
}
