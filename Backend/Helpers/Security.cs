using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens; 
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;
using System.Text;
using Backend.DBClasses;
using Backend.Endpoints;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using System.Text.RegularExpressions;

namespace Backend.Helpers
{
    public static class SecurityHelper
    {
        public static string CripteazaParola(string parola)
        {
            return BCrypt.Net.BCrypt.HashPassword(parola);
        }

        public static bool VerificaParola(string stringIntrodus, string stringCriptat)
        {
            return BCrypt.Net.BCrypt.Verify(stringIntrodus, stringCriptat);
        }

        public static string CripteazaCNP(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString().Substring(0, 13);
            }
        }

        public static bool VerificaCnp(string cnpIntrodus, string cnpCriptatInDb)
        {
            return CripteazaCNP(cnpIntrodus) == cnpCriptatInDb;
        }

        public static string CreareJWTLogin(Utilizator utilizator, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var cheieSecreta = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                            new Claim(ClaimTypes.NameIdentifier, utilizator.Id.ToString()),
                            new Claim(ClaimTypes.Email, utilizator.Email),
                            new Claim(ClaimTypes.Name, utilizator.Nume),
                            new Claim("Prenume", utilizator.Prenume)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(cheieSecreta),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        public static async Task<string?> GetRol(int utilizatorId, string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            var parametrii = new DynamicParameters();
            parametrii.Add("@id", utilizatorId);

            return await connection.QueryFirstOrDefaultAsync<string>(
                "sp_Utilizator_Get_Rol",
                parametrii,
                commandType: CommandType.StoredProcedure);
        }

        public static async Task<Companie?> GetCompanie(int utilizatorId, string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            var parametrii = new DynamicParameters();
            parametrii.Add("@id", utilizatorId);

            return await connection.QueryFirstOrDefaultAsync<Companie>(
                "sp_Utilizator_Get_Companie",
                parametrii,
                commandType: CommandType.StoredProcedure);
        }

    //DE SCURTAT, SA LE FAC SA CONVEARGA DOAR INTR-O SINGURA FUNCTIE PE AMANTREI
        public static async Task<IResult?> VerificaAdminGeneral(ClaimsPrincipal admin, IConfiguration config)
        {
            var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idString, out int idAdmin))
                return Results.Unauthorized();

            var connectionString = config.GetConnectionString("DefaultConnection");
            var rolAdmin = await GetRol(idAdmin, connectionString);

            Console.WriteLine("Rolul pe care toti il asteptam este AdminGeneral " + rolAdmin);
            if (rolAdmin != "AdminGeneral")
                return Results.Forbid();

            return null;
        }

        //DE SCURTAT, SA LE FAC SA CONVEARGA DOAR INTR-O SINGURA FUNCTIE PE AMANTREI
        public static async Task<IResult?> VerificaAdminLocal(ClaimsPrincipal admin, IConfiguration config)
        {
            var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idString, out int idAdmin))
                return Results.Unauthorized();

            var connectionString = config.GetConnectionString("DefaultConnection");
            var rolAdmin = await GetRol(idAdmin, connectionString);

            Console.WriteLine("Rolul pe care toti il asteptam este AdminCompanie " + rolAdmin);
            if (rolAdmin != "AdminCompanie")
                return Results.Forbid();

            return null;
        }

        //DE SCURTAT, SA LE FAC SA CONVEARGA DOAR INTR-O SINGURA FUNCTIE PE AMANTREI
        public static async Task<IResult?> VerificaAdminFurnizor(ClaimsPrincipal admin, IConfiguration config)
        {
            var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idString, out int idAdmin))
                return Results.Unauthorized();

            var connectionString = config.GetConnectionString("DefaultConnection");
            var rolAdmin = await GetRol(idAdmin, connectionString);

            Console.WriteLine("Rolul pe care toti il asteptam este AdminFurnizor :" + rolAdmin);
            if (rolAdmin != "AdminFurnizor")
                return Results.Forbid();

            return null;
        }



        public static void AdaugaEroare(Dictionary<string, List<string>> erori, string camp, string mesaj)
        {
            if (!erori.ContainsKey(camp)) erori[camp] = new List<string>();
            erori[camp].Add(mesaj);
        }

        public static Dictionary<string, List<string>> ValideazaDateCompanie(Companie companie)
        {
            var erori = new Dictionary<string, List<string>>();

            if (!Validators.EsteEmailValid(companie.Email))
                AdaugaEroare(erori, "email", "Email-ul nu are formatul corect!");

            if (!Validators.EsteNumarTelefonValid(companie.Numar_Telefon))
                AdaugaEroare(erori, "numar_telefon", "Numarul de telefon trebuie sa contina fix 10 cifre!");

            if (!Validators.EsteNumeCompanieValid(companie.Nume_Companie))
                AdaugaEroare(erori, "nume_companie", "Trebuie sa completezi ceva / Ai voie numai cu litere!");

            return erori;
        }

        //daca mi da eroare cand dau dotnet run, sa fiu atent aici, suta la suta crapa si tre sa includ ceva
        public static Dictionary<string, List<string>> ValideazaDateFurnizor(AdminGestioneazaFurnizorRequest furnizorNou)
        {
            var erori = new Dictionary<string, List<string>>();

            if (!Validators.EsteEmailValid(furnizorNou.email_furnizor))
                AdaugaEroare(erori, "email", "Email-ul nu are formatul corect!");

            if (!Validators.EsteNumarTelefonValid(furnizorNou.numar_telefon))
                AdaugaEroare(erori, "numar_telefon", "Numarul de telefon trebuie sa contina fix 10 cifre!");

            if (!Validators.EsteNumeCompanieValid(furnizorNou.nume_furnizor))
                AdaugaEroare(erori, "nume_furnizor", "Trebuie sa completezi ceva / Ai voie numai cu litere!");

            return erori;
        }

        public static Dictionary<string, List<string>> ValideazaDatePiesa(Piese piesaNoua)
        {
            var erori = new Dictionary<string, List<string>>();

            
            if (!Validators.EsteNumeCompanieValid(piesaNoua.Nume_Piesa))
                AdaugaEroare(erori, "nume_furnizor", "Trebuie sa completezi ceva / Ai voie numai cu litere!");

            if (!Validators.EstePretValid(piesaNoua.Pret_Cumparare))
                AdaugaEroare(erori, "pret_piesa", "Trebuie sa pui un numar valid, pozitiv, fara litere!");

            return erori;
        }

        public static Dictionary<string, List<string>> ValideazaDateAdaugaPiesa(AdaugaPiesaRequest adaugaPiesa)
        {
            var erori = new Dictionary<string, List<string>>();

            if (adaugaPiesa.Cantitate <= 0)
            {
                AdaugaEroare(erori, "cantitate", "Cantitatea trebuie să fie mai mare ca 0!");
            }
            if (!string.IsNullOrWhiteSpace(adaugaPiesa.DetaliiPiese) && adaugaPiesa.DetaliiPiese.Length > 255)
            {
                AdaugaEroare(erori, "detalii_piese", "Detaliile piesei sunt prea lungi (maxim 255 caractere)!");
            }

            return erori;
        }

        //poate aici un failsafe sa vedem daca pretul de vanzare al meu este mai mic ca cel de cumparare al piesei ---> eventuale pierderi din partea noastra?
        public static Dictionary<string, List<string>> ValideazaPretVanzare(decimal? Pret_Vanzare)
        {
            var erori = new Dictionary<string, List<string>>();

            if (!Validators.EstePretValid(Pret_Vanzare))
                AdaugaEroare(erori, "pret_piesa", "Trebuie sa pui un numar valid, pozitiv, fara litere!");

            return erori;
        }

        public static (string? emailCautare, int? idCautare, string? cnpHash) ParseazaIdentificatorCompanie(string identificator, Dictionary<string, List<string>> erori)
        {
            string? emailCautare = null;
            int? idCautare = null;
            string? cnpHash = null;

            Console.WriteLine("Identificatorul bagat in functie este " + identificator);
            Console.WriteLine("Daca e CNP, atunci asta trebuie sa fie adevarata  = " + Validators.EsteCnpValid(identificator) + " si " + identificator.Length);
            if (string.IsNullOrWhiteSpace(identificator) || identificator.Contains("***"))
                return (emailCautare, idCautare, cnpHash);

            if (identificator.Contains("@"))
            {
                if (!Validators.EsteEmailValid(identificator))
                    AdaugaEroare(erori, "identificator", "Email-ul introdus nu are un format valid!");
                else
                    emailCautare = identificator;
            }
            else if (identificator.Trim().Length == 13)
            {
                if (!Validators.EsteCnpValid(identificator.Trim()))
                    AdaugaEroare(erori, "identificator", "CNP-ul introdus nu este valid (trebuie să respecte formatul oficial de 13 cifre)!");
                else
                    cnpHash = CripteazaCNP(identificator);
            }
            else if (identificator.Length < 10 && int.TryParse(identificator, out int idParsat))
            {
                idCautare = idParsat;
            }
            else
            {
                AdaugaEroare(erori, "identificator", "Trebuie sa introduci un Email, CNP, sau ID de format acceptat!");
            }
            return (emailCautare, idCautare, cnpHash);
        }

        public static async Task<(IResult? Eroare, Companie? CompanieGasita)> ObtineCompanieAdminLocal(ClaimsPrincipal admin, string connectionString)
        {
            var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idString, out int idAdmin))
                return (Results.Unauthorized(), null);


            using (var connection = new SqlConnection(connectionString))
            {
                var parametruCompanie = new DynamicParameters();
                parametruCompanie.Add("@id", idAdmin);

                var companieLocalAdmin = await connection.QueryFirstOrDefaultAsync<Companie>(
                    "sp_Utilizator_Get_Companie",
                    parametruCompanie,
                    commandType: CommandType.StoredProcedure
                );

                if (companieLocalAdmin == null)
                    return (Results.BadRequest(new { message = "Acest utilizator nu are nicio companie atribuită!" }), null);

                return (null, companieLocalAdmin);
            }
        }

        public static async Task<(IResult? Eroare, Furnizor? FurnizorGasit, int? idAdminFurnizor)> ObtineFurnizorAdminLocal(ClaimsPrincipal admin, string connectionString)
        {
            var idString = admin.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idString, out int idAdmin))
                return (Results.Unauthorized(), null, null);


            using (var connection = new SqlConnection(connectionString))
            {
                var parametruUtilizator = new DynamicParameters();
                parametruUtilizator.Add("@idAdminFurnizor", idAdmin);

                var companieLocalAdmin = await connection.QueryFirstOrDefaultAsync<Furnizor>(
                    "sp_Furnizor_GetFurnizorByAdmin",
                    parametruUtilizator,
                    commandType: CommandType.StoredProcedure
                );

                if (companieLocalAdmin == null)
                    return (Results.BadRequest(new { message = "Acest utilizator nu are nicio companie atribuită!" }), null,null);

                return (null, companieLocalAdmin, idAdmin);
            }
        }

        public static async Task<(IResult? Eroare, Utilizator? Utilizator, string? Rol, Companie? Companie)> ObtineContextDinJWT(
            ClaimsPrincipal utilizatorCompanie,
            string connectionString)
        {
            var idString = utilizatorCompanie.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idString, out int idUtilizatorLogat))
                return (Results.Unauthorized(), null, null, null);

            using (var connection = new SqlConnection(connectionString))
            {
                var parametrii = new DynamicParameters();
                parametrii.Add("@id", idUtilizatorLogat);

                var utilizator = await connection.QueryFirstOrDefaultAsync<Utilizator>(
                    "sp_Utilizator_getbyID",
                    parametrii,
                    commandType: CommandType.StoredProcedure);

                if (utilizator != null)
                {
                    string jsonUtilizator = JsonSerializer.Serialize(utilizator, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine("=== DATE UTILIZATOR EXTRASE DIN BAZA DE DATE ===");
                    Console.WriteLine(jsonUtilizator);
                    Console.WriteLine("================================================");
                }
                else
                {
                    Console.WriteLine("Utilizatorul nu a fost găsit în baza de date!");
                    return (Results.BadRequest(new { message = "Trebuie sa fie logat, sau nu exista" }), null, null, null);
                }

                string? rolUtilizator = await connection.QueryFirstOrDefaultAsync<string>(
                    "sp_Utilizator_Get_Rol",
                    parametrii,
                    commandType: CommandType.StoredProcedure
                );

                Companie? companieUtilizator = null;

                if (rolUtilizator != "AdminFurnizor")
                {
                    companieUtilizator = await connection.QueryFirstOrDefaultAsync<Companie>(
                        "sp_Utilizator_Get_Companie",
                        parametrii,
                        commandType: CommandType.StoredProcedure
                    );
                }
                else
                {
                    Console.WriteLine("ESTE FURNIZOR, NU POATE SA COMANDE!!!");
                    return (Results.BadRequest(new { message = "Furnizorii nu au voie aici" }), null, null, null);
                }

                if (companieUtilizator != null)
                {
                    string jsonCompanie = JsonSerializer.Serialize(companieUtilizator, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine("=== DATE COMPANIE EXTRASE DIN BAZA DE DATE ===");
                    Console.WriteLine(jsonCompanie);
                    Console.WriteLine("================================================");
                }
                else
                {
                    Console.WriteLine("Compania nu a fost găsit în baza de date!");
                    return (Results.BadRequest(new { message = "Nu pot sa intru aici, nu exista companie" }), null, null, null);
                }

                return (null, utilizator, rolUtilizator, companieUtilizator);
            }
        }

        public static async Task<(IResult? Eroare, Comanda? ComandaGasita)> VerificaSiObtineComandaDupaId(
            int idComanda,
            int idCompanie,
            string connectionString,
            bool verificaFinalizargite = true)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var parametruComanda = new DynamicParameters();
                parametruComanda.Add("@idComanda", idComanda);
                parametruComanda.Add("@idCompanie", idCompanie);

                var comandaCeruta = await connection.QueryFirstOrDefaultAsync<Comanda>(
                    "sp_Comanda_Companie_GetComandaById",
                    parametruComanda,
                    commandType: CommandType.StoredProcedure);

                if (comandaCeruta == null)
                    return (Results.BadRequest(new { message = "Nu exista comanda ceruta!" }), null);

                if (verificaFinalizare && comandaCeruta.stadiu_finalizare == true)
                    return (Results.BadRequest(new { message = "Comanda e deja plasata, nu poti sa adaugi o piesa intr-o comanda deja depusa" }), null);

                return (null, comandaCeruta);
            }
        }
    }
}