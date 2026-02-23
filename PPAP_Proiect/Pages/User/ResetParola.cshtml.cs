using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using PPAP_Proiect.Data;
using PPAP_Proiect.Models;
using PPAP_Proiect.Services;

namespace PPAP_Proiect.Pages.User
{
    public class ResetParolaModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly DBConectare _db;
        private readonly FunctiiUtilizator _fu;
        private readonly UserEmailService _ues;

        [BindProperty]
        public string tokenResetare { get; set; }
        [BindProperty]
        public int UserId { get; set; }

        [BindProperty]
        public string parolaSchimbare { get; set; }
        [BindProperty]
        public string parolaSchimbareConfirmare { get; set; }

        public ResetParolaModel(IConfiguration config, DBConectare db, FunctiiUtilizator functiiUtilizator, UserEmailService ues)
        {
            _config = config;
            _db = db;
            _fu = functiiUtilizator;
            _ues = ues;
        }


        public string getConfigReset()
        {
            return _config["SecretKeys:resetareparolaCont"];
        }

        public IActionResult OnGet()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId != null)
            {
                HttpContext.Session.SetString("mesajEroare", "Nu ai acces, modifica parola din panoul de edit!");
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public IActionResult OnPost(string emailsaucnp)
        {
            Utilizator user = null;

            if (string.IsNullOrEmpty(emailsaucnp))
            {
                ModelState.AddModelError("mesajEroare", "Camp gol!");
            }

            int emailValid = FunctiiUtilizator.IsValidEmail(emailsaucnp);
            int cnpValid = FunctiiUtilizator.IsValidCnp(emailsaucnp);

            if(emailValid != 1 && cnpValid != 1)
            {
                ModelState.AddModelError("mesajEroareCNPsauEmail", "Input invalid!");
            }

            try
            {
               user = _fu.GetUserByEmailsauCNP(emailsaucnp);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("mesajEroare", "Eroare : " + e.Message);
            }

            if (user == null)
            {
                ModelState.AddModelError("mesajEroare", "Utilizator inexistent in baza noastra de date");
                return Page();
            }

            string configReset = getConfigReset();

            tokenResetare = FunctiiUtilizator.HashToken(user.Nume, user.Cnp, configReset);
            UserId = user.Id;
            string linkConfirmare = Url.Page(
                "/User/ResetParola",
                pageHandler: "Resetare",
                values: new { token = tokenResetare, userId = user.Id },
                protocol: Request.Scheme
                );

            string numeUser = $"{user.Nume} {user.Prenume}";
            _ues.TrimiteResetaerParola(numeUser, user.Email, linkConfirmare);

            HttpContext.Session.SetString("mesajSucces", "Email de resetare trimis pe mailul " + user.Email);
            return RedirectToPage("/User/Login");
        }

        public IActionResult OnGetResetare(string token, int? userId)
        {

            
            if (userId == null)
            {
                return RedirectToPage("/User/Login");
            }

            Utilizator user = _fu.GetUtilizatorSession((int)userId);


            if (user == null)
            {
                HttpContext.Session.SetString("eroareLogin", "Ceva nu a mers bine!");
                return RedirectToPage("/User/Login");
            }

            UserId = (int)userId;

            tokenResetare = FunctiiUtilizator.HashToken(user.Nume, user.Cnp, this.getConfigReset());

            if (!string.Equals(token, tokenResetare))
            {
                ModelState.AddModelError("mesajEroare", "Token-urile nu sunt egale!");
                return Page();

            }

            return Page();

        }

        public IActionResult OnPostResetare()
        {
            int userId = UserId;
            string token;

            Utilizator user = _fu.GetUtilizatorSession((int)userId);


            if (user == null)
            {
                HttpContext.Session.SetString("eroareLogin", "Ceva nu a mers bine!");
                return RedirectToPage("/User/Login");
            }

            token = FunctiiUtilizator.HashToken(user.Nume, user.Cnp, this.getConfigReset());

            if (!string.Equals(token, tokenResetare))
            {
                HttpContext.Session.SetString("eroareLogin", "Token-urile nu sunt egale, mai incearca odata!");
                Console.WriteLine("tokenul din clasa este egal cu " + tokenResetare + " si tokenul format este egal cu " + token);
                return RedirectToPage("/User/Login");

            }
            int parolaValida = FunctiiUtilizator.IsValidPassword(parolaSchimbare);
            int parolaEgala = FunctiiUtilizator.PasswordEqual(parolaSchimbare, parolaSchimbareConfirmare);

            switch (parolaValida)
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

            if (parolaEgala == 0 && parolaValida == 1)
                ModelState.AddModelError("eroareParolaConfirmare", "Cele doua parole puse de tine nu sunt egale!");

            if (!ModelState.IsValid)
                return Page();

            string parolaHash = FunctiiUtilizator.HashParola(parolaSchimbare);

            using (SqlConnection conn = _db.GetConnection())
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                    conn.Open();

                using (SqlCommand cmd = new SqlCommand("sp_Utilizator_ResetParola", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@parola", parolaHash);
                    cmd.Parameters.AddWithValue("@userId", userId); //care nu exista, uof
                    var returnParam = new SqlParameter();
                    returnParam.Direction = System.Data.ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnParam);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        int result = (int)returnParam.Value;

                        switch (result)
                        {
                            case 0:
                                ModelState.AddModelError("mesajEroare", "Ceva nu a mers bine!");
                                Console.WriteLine("Ceva nu a mers bine la prola bagmias pula in ea de parola dracu!");
                                return Page();
                        }
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("mesajEroare", "Eroare: " + e.Message);
                        Console.WriteLine(e.Message);
                        return Page();
                    }
                }
            }
            HttpContext.Session.SetString("mesajSucces", "Parola resetata cu succes!");
            return RedirectToPage("/User/Login");
        }
    }
}
