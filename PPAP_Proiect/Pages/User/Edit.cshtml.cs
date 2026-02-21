using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Security;
using PPAP_Proiect.Data;
using PPAP_Proiect.Models;

namespace PPAP_Proiect.Pages.User
{
    public class EditModel : PageModel
    {
        private readonly DBConectare _db;
        private readonly FunctiiUtilizator _fu;
        public EditModel (DBConectare db, FunctiiUtilizator fu)
        {
            _db = db;
			_fu = fu;
        }

        [BindProperty]
        public Utilizator userEdit { get; set; }
        [BindProperty]
        public string parolaConfirmare { get; set;  }
        public IActionResult OnGet()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if(userId == null)
            {
                return RedirectToPage("/Index");
            }

			userEdit = _fu.GetUtilizatorSession((int)userId);
            return Page();
        }

        public IActionResult OnPost()
        {

			int? userId = HttpContext.Session.GetInt32("userId");
			if (userId == null) return RedirectToPage("/Index");

			int prenumeValid = FunctiiUtilizator.IsValidNume(userEdit.Prenume);
            int numeValid = FunctiiUtilizator.IsValidNume(userEdit.Nume);
            int cnpValid = FunctiiUtilizator.IsValidCnp(userEdit.Cnp);
            int emailValid = FunctiiUtilizator.IsValidEmail(userEdit.Email);

			if (prenumeValid == 0)
				ModelState.AddModelError("eroarePrenume", "Prenumele trebuie sa contina doar litere!");


			if (numeValid == 0)
				ModelState.AddModelError("eroareNume", "Numele trebuie sa contina doar litere!");


			switch (cnpValid)
			{
				case -2:
					ModelState.AddModelError("eroareCNP", "CNP-ul nu trebuie sa contina litere!");
					break;
				case 0:
					ModelState.AddModelError("eroareCNP", "CNP-ul trebuie sa aiba lungimea 13!");
					break;
				default:
					break;
			}

			switch (emailValid)
			{
				case 0:
					ModelState.AddModelError("eroareEmail", "Email-ul trebuie sa fie sub forma de 'xxxx@xxxx.xxx' !");
					break;

			}

			if (!string.IsNullOrEmpty(userEdit.Parola))
			{
				int parolaValida = FunctiiUtilizator.IsValidPassword(userEdit.Parola);
				int parolaConfirmata = FunctiiUtilizator.PasswordEqual(parolaConfirmare, userEdit.Parola);

				switch (parolaValida)
				{
					case -2:
						ModelState.AddModelError("eroareParola", "Parola trebuie sa aiba o lungime de cel putin 8 caractere!");
						break;
					case 0:
						ModelState.AddModelError("eroareParola", "Parola trebuie sa aiba un caracter de tip A-Z, unul de tip a-z si unul special (ex. @!#$%....)!");
						break;
				}

				if (parolaConfirmata == 0 && parolaValida == 1)
					ModelState.AddModelError("eroareParolaConfirmare", "Cele doua parole puse de tine nu sunt egale!");
			}
			else
			{
				ModelState.Remove("userEdit.Parola");
				ModelState.Remove("parolaConfirmare");
			}

			if (!ModelState.IsValid)
				return Page();


			string parolaHash;

			if (string.IsNullOrEmpty(userEdit.Parola))
			{
				parolaHash = _fu.GetUtilizatorSession((int)userId).Parola;
			}
			else
			{
				parolaHash = FunctiiUtilizator.HashParola(userEdit.Parola);
			}

			using (SqlConnection conn = _db.GetConnection())
			{
				if (conn.State == System.Data.ConnectionState.Closed)
					conn.Open();

				using (SqlCommand cmd = new SqlCommand("sp_Utilizator_Edit", conn))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("@email", userEdit.Email);
					cmd.Parameters.AddWithValue("@nume", userEdit.Nume);
					cmd.Parameters.AddWithValue("@prenume", userEdit.Prenume);
					cmd.Parameters.AddWithValue("@cnp", userEdit.Cnp);
					cmd.Parameters.AddWithValue("@parola", parolaHash);
					cmd.Parameters.AddWithValue("@userId", userId);

					try { 
					cmd.ExecuteNonQuery();
					}
					catch(Exception e)
					{
						ModelState.AddModelError("eroareBazaDeDate", "Eroare: " + e.Message);
						return Page();
					}
				}
			}

			return RedirectToPage("/Index" , new { status = "succes_edit"});
		}
    }
}
