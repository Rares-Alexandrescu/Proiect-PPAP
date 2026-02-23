using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using PPAP_Proiect.Data;
using PPAP_Proiect.Models;
using PPAP_Proiect.Services;

namespace PPAP_Proiect.Pages.User
{
    public class EmailConfirmareModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly DBConectare _db;
        private readonly FunctiiUtilizator _fu;
        private readonly UserEmailService _ues;

        public string tokenConfirmare; 
        public EmailConfirmareModel(IConfiguration config, DBConectare db, FunctiiUtilizator fu, UserEmailService ues)
        {
            _config = config;
            _db = db;
            _fu = fu;
            _ues = ues;
        }

        public Utilizator userCurent;
        public IActionResult OnGet()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if(userId == null)
            {
                return RedirectToPage("/Index");
            }


            userCurent = _fu.GetUtilizatorSession((int)userId);

            if(userCurent.ContVerificat == true)
            {
				HttpContext.Session.SetString("mesajSucces", "Cont deja verificat!");
				return RedirectToPage("/Index");
            }

            string configVerificare = _config["SecretKeys:confirmareCont"];
		    tokenConfirmare = FunctiiUtilizator.HashToken(userCurent.Nume, userCurent.Cnp, configVerificare);

			string linkConfirmare = Url.Page(
	        "/User/EmailConfirmare",
	        pageHandler: "Confirm",
	        values: new { token = tokenConfirmare },
	        protocol: Request.Scheme
            );


			string numeUser = $"{userCurent.Nume} {userCurent.Prenume}";
            _ues.TrimitereConfirmareCont(numeUser, userCurent.Email, linkConfirmare);

			HttpContext.Session.SetString("mesajSucces", "Email de confirmare trimis pe mailul " + userCurent.Email + "!");
			return RedirectToPage("/Index");
		}

        public IActionResult OnGetConfirm(string token)
        {
			int? userId = HttpContext.Session.GetInt32("userId");

			if (userId == null)
				return RedirectToPage("/Index");

            var user = _fu.GetUtilizatorSession((int)userId);

            if(user.ContVerificat == true)
            {
                HttpContext.Session.SetString("mesajSucces", "Cont deja verificat!");
                return RedirectToPage("/Index");
            }

            string configVerificare = _config["SecretKeys:confirmareCont"];
            string tokenFormat = FunctiiUtilizator.HashToken(user.Nume, user.Cnp, configVerificare);

            if(token != tokenFormat)
            {
				HttpContext.Session.SetString("mesajEroare", "Tokenul dat de tine nu este bun! Incearca din nou pe noul mail trimis!");
                Console.WriteLine(HttpContext.Session.GetString("mesajEroare"));
				return RedirectToPage("/Index");
            }
            else {
                using (SqlConnection conn = _db.GetConnection())
                {
                    if (conn.State == System.Data.ConnectionState.Closed)
                        conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_Utilizator_Confirm", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id", user.Id);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch(Exception e)
                        {
							HttpContext.Session.SetString("mesajEroare", "Eroare : " + e.Message);
                            Console.WriteLine(e.Message);
							return RedirectToPage("/Index");
                        }
                    }
                }
            }

			HttpContext.Session.SetString("mesajSucces", "Cont confirmat cu succes!");
            Console.WriteLine("Ura");
            return RedirectToPage("/Index");
		}
    }
}
