using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using PPAP_Proiect.Data;
using PPAP_Proiect.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Specialized;
using Org.BouncyCastle.Pqc.Crypto.Utilities;

namespace PPAP_Proiect.Pages.User
{
    public class RegisterModel : PageModel
    {

        private readonly DBConectare _db;

        public RegisterModel(DBConectare db)
        {
            _db = db;
        }

        [BindProperty]
        public Utilizator userNou { get; set; } = new Utilizator();

        [BindProperty]
        public string parolaConfirmare { get; set; }
        [TempData]
        public string mesajSucces { get; set; }
        public IActionResult OnGet()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if(userId != null)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            int prenumeValid = FunctiiUtilizator.IsValidNume(userNou.Prenume);
            int numeValid = FunctiiUtilizator.IsValidNume(userNou.Nume);
            int cnpValid = FunctiiUtilizator.IsValidCnp(userNou.Cnp);
            int emailValid = FunctiiUtilizator.IsValidEmail(userNou.Email);
            int parolaValida = FunctiiUtilizator.IsValidPassword(userNou.Parola);
            int parolaConfirmata = FunctiiUtilizator.PasswordEqual(parolaConfirmare, userNou.Parola);
            Console.WriteLine("parola prima = " + userNou.Parola + " si aia de confirmare = " + parolaConfirmare);
            
            if (prenumeValid == 0)
                ModelState.AddModelError("eroarePrenume", "Prenumele trebuie sa contina doar litere!");

            if (prenumeValid == -1)
                ModelState.AddModelError("eroarePrenume", "Camp Prenume este gol!");

            if (numeValid == 0)
                ModelState.AddModelError("eroareNume", "Numele trebuie sa contina doar litere!");

            if (numeValid == -1)
                ModelState.AddModelError("errorNume", "Camp Nume este gol!");

            switch(cnpValid)
            {
                case -1:
                    ModelState.AddModelError("eroareCNP", "Camp CNP este gol!");
                    break;
                case -2:
                    ModelState.AddModelError("eroareCNP", "CNP-ul nu trebuie sa contina litere!");
                    break;
                case 0:
                    ModelState.AddModelError("eroareCNP", "CNP-ul trebuie sa aiba lungimea 13!");
                    break;
                default:
                    break;
            }

            switch(emailValid)
            {
                case -1:
                    ModelState.AddModelError("eroareEmail", "Camp Email este gol!");
                    break;
                case 0:
                    ModelState.AddModelError("eroareEmail", "Email-ul trebuie sa fie sub forma de 'xxxx@xxxx.xxx' !");
                    break;

            }

            switch(parolaValida)
            {
                case -1:
                    ModelState.AddModelError("eroareParola", "Camp parola este gol!");
                    break;
                case -2:
                    ModelState.AddModelError("eroareParola", "Parola trebuie sa aiba o lungime de cel putin 8 caractere!");
                    break;
                case 0:
                    ModelState.AddModelError("eroareParola", "Parola trebuie sa aiba un caracter de tip A-Z, unul de tip a-z si unul special (ex. @!#$%....)!");
                    break;
            }

            if (parolaConfirmata == 0 && parolaValida == 1)
                ModelState.AddModelError("eroareParolaConfirmare", "Cele doua parole puse de tine nu sunt egale!");

            if (!ModelState.IsValid)
                return Page();

            string parolaHash = FunctiiUtilizator.HashParola(userNou.Parola);

            using (SqlConnection conn = _db.GetConnection())
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                    conn.Open();

                using (SqlCommand emailExista = new SqlCommand("sp_Utilizator_Exista_Email", conn))
                {
                    emailExista.CommandType = System.Data.CommandType.StoredProcedure;
                    emailExista.Parameters.AddWithValue("@email", userNou.Email);

                    var returnParam = new SqlParameter();
                    returnParam.Direction = System.Data.ParameterDirection.ReturnValue;
                    emailExista.Parameters.Add(returnParam);

                    try
                    {
                        emailExista.ExecuteNonQuery();
                    }
					catch (Exception e)
					{
						ModelState.AddModelError("eroareBazaDeDate", "Eroare : " + e.Message);
						return Page();
					}
					int result = (int)returnParam.Value;
                    if(result == 0)
                    {
                        ModelState.AddModelError("eroareEmail", "Email-ul este deja asociat altui cont!");
                    }
                }

				using (SqlCommand cnpExista = new SqlCommand("sp_Utilizator_Exista_CNP", conn))
				{
                    cnpExista.CommandType = System.Data.CommandType.StoredProcedure;
                    cnpExista.Parameters.AddWithValue("@cnp", userNou.Cnp);

					var returnParam = new SqlParameter();
					returnParam.Direction = System.Data.ParameterDirection.ReturnValue;
					cnpExista.Parameters.Add(returnParam);
                    try
                    {
                        cnpExista.ExecuteNonQuery();
                    }
                    catch(Exception e)
                    {
                        ModelState.AddModelError("eroareBazaDeDate", "Eroare : " +  e.Message);
                        return Page();
                    }
					int result = (int)returnParam.Value;
					if (result == 1)
					{
						ModelState.AddModelError("eroareCNP", "CNP-ul este deja asociat altui cont!");
					}
				}

                if(!ModelState.IsValid)
                {
                    return Page();
                }

				using (SqlCommand cmd = new SqlCommand("sp_Utilizator_Register_Site", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nume", userNou.Nume);
					cmd.Parameters.AddWithValue("@prenume", userNou.Prenume);
					cmd.Parameters.AddWithValue("@email", userNou.Email);
					cmd.Parameters.AddWithValue("@cnp", userNou.Cnp);
					cmd.Parameters.AddWithValue("@parola", parolaHash);

                    var returnParam = new SqlParameter();
                    returnParam.Direction = System.Data.ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnParam);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        int result = (int)returnParam.Value;

						switch (result)
						{
							case -1:
								ModelState.AddModelError("eroareEmail", "Email-ul este deja asociat altui cont!");
								return Page();

							case -2:
								ModelState.AddModelError("eroareCNP", "CNP-ul este deja asociat altui cont!");
								return Page();
                        }

						}
                    catch (Exception e)
                    {
                        ModelState.AddModelError("eroareBazaDeDate", "Eroare = " + e.Message); 
                        return Page();
                    }
				}
            }
            return RedirectToPage("/User/Login", new { status = "succes"});
        }
    }
}
