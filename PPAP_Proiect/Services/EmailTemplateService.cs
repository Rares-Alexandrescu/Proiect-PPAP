using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PPAP_Proiect.Services
{
	public class EmailTemplateService
	{
		private readonly string _templateFolder;

		public EmailTemplateService()
		{
			_templateFolder = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates");
		}

		public string GetTemplate(string templateName)
		{
			var path = Path.Combine(_templateFolder, templateName + ".html");

			if(!File.Exists(path))
			{
				throw new FileNotFoundException($"Nu a fost gasit '{templateName}'");
			}

			return File.ReadAllText(path);
		}
	}
}
