using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using PPAP_Proiect.Data;

namespace PPAP_Proiect.Pages.User
{
    public class LoginModel : PageModel
    {
		private readonly DBConectare _db;

		public LoginModel(DBConectare db)
		{
			_db = db;
		}

		[BindProperty]
		public string EmailsauCNP { get; set; }

		[BindProperty]
		public string parola { get; set; }

		public IActionResult OnGet(string status)
		{
			int? userId = HttpContext.Session.GetInt32("userId");
			ModelState.Remove("mesajSucces");

			if(status == "succes")
			{
				ModelState.AddModelError("mesajSucces", "Cont creat cu succes!");
			}
			if (userId != null)
			{
				return RedirectToPage("/Index");
			}

			return Page();
		}

		public IActionResult OnPost()
		{

			if(EmailsauCNP.IsNullOrEmpty())
			{
				ModelState.AddModelError("eroareEmailCNP", "Email/CNP nu au fost adaugate!");
			}
			
			if(parola.IsNullOrEmpty())
			{
				ModelState.AddModelError("eroareParola", "Parola nu este adaugata!");
			}

			if (!ModelState.IsValid)
			{
				return Page();
			}

			using (SqlConnection conn = _db.GetConnection())
			{
				if (conn.State == System.Data.ConnectionState.Closed)
					conn.Open();

				using(SqlCommand cmd = new SqlCommand ("sp_Utilizator_Login", conn))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("@email", EmailsauCNP);
					cmd.Parameters.AddWithValue("@cnp", EmailsauCNP);
					cmd.Parameters.AddWithValue("@parola", FunctiiUtilizator.HashParola(parola));

					using(SqlDataReader reader = cmd.ExecuteReader())
					{
						if(reader.Read())
						{
							int userId = reader.GetInt32(reader.GetOrdinal("id"));
							HttpContext.Session.SetInt32("userId", userId);
							ModelState.AddModelError("mesajSucces", "Logat cu succes!");
							return RedirectToPage("/Index", new { status = "succes" });
						}
						else
						{
							ModelState.AddModelError("eroareLogin", "Email/CNP sau parola gresita!");
							return Page();
						}
					}
				}

			}
		}
	}
}
