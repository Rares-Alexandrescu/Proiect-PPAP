using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1;
using PPAP_Proiect.Models;

namespace PPAP_Proiect.Data
{
	public class FunctiiUtilizator
	{
		private readonly DBConectare _db;

		public FunctiiUtilizator(DBConectare db)
		{
			_db = db;
		}
		public static string HashParola(string parola)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(parola));
				StringBuilder builder = new StringBuilder();
				foreach (var b in bytes)
					builder.Append(b.ToString("x2"));
				return builder.ToString();
			}
		}

		public static string HashToken(string Nume, string Cnp, string cheieSecreta)
		{
			string data = $"{Nume}|{Cnp}";

			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(cheieSecreta)))
			{
				byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
				return Convert.ToBase64String(hashBytes);
			}
		}
		public static int IsValidEmail(string email)
		{
			if (string.IsNullOrEmpty(email)) return -1;
			string regex_email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			return Regex.IsMatch(email, regex_email) ? 1 : 0;
		}

		public static int IsValidCnp(string cnp)
		{
			if (string.IsNullOrEmpty(cnp)) return -1;
			foreach (var c in cnp)
			{
				if (!char.IsNumber(c))
					return -2;
			}
			return cnp.Length == 13 ? 1 : 0;
		}

		public static int IsValidPassword(string parola)
		{
			if (string.IsNullOrEmpty(parola)) return -1;

			if (parola.Length < 8) return -2;
			bool caracterSpecial = false;
			bool literaMare = false;
			bool literaMica = false;
			bool cifra = false;

			foreach (var c in parola)
			{
				if (char.IsLower(c))
					literaMica = true;
				else if (char.IsUpper(c))
					literaMare = true;
				else if (char.IsNumber(c))
					cifra = true;
				else
					caracterSpecial = true;
			}

			if (caracterSpecial & literaMare & literaMica & cifra)
				return 1;
			else
				return 0;
		}

		public static int IsValidNume(string nume)
		{
			if (string.IsNullOrEmpty(nume)) return -1;

			foreach (char c in nume)
			{
				if (!char.IsLetter(c))
				{
					return 0;
				}
			}

			return 1;
		}
		public static int PasswordEqual(string parola1, string parola2)
		{
			return string.Equals(parola2, parola1) ? 1 : 0;
		}

		public Utilizator GetUtilizatorSession(int userId)
		{
			using (SqlConnection conn = _db.GetConnection())
			{
				if (conn.State == System.Data.ConnectionState.Closed)
					conn.Open();

				using (SqlCommand cmd = new SqlCommand("sp_Utilizator_getbyID", conn))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					cmd.Parameters.AddWithValue("@id", userId);

					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						if (!reader.Read())
							return null;

						return new Utilizator
						{
							Id = reader.GetInt32(reader.GetOrdinal("id")),
							Nume = reader.GetString(reader.GetOrdinal("nume")),
							Prenume = reader.GetString(reader.GetOrdinal("prenume")),
							Email = reader.GetString(reader.GetOrdinal("email")),
							Cnp = reader.GetString(reader.GetOrdinal("cnp")),
							RolId = reader.IsDBNull(reader.GetOrdinal("rol_id"))
								? 0
								: reader.GetInt32(reader.GetOrdinal("rol_id")),
							CompanieId = reader.IsDBNull(reader.GetOrdinal("companie_id"))
								? 0
								: reader.GetInt32(reader.GetOrdinal("companie_id")),

													ContVerificat = !reader.IsDBNull(reader.GetOrdinal("cont_verificat"))
								&& reader.GetBoolean(reader.GetOrdinal("cont_verificat")),

													CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
								? DateTime.Now
								: reader.GetDateTime(reader.GetOrdinal("created_at")),

													UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
								? (DateTime?)null
								: reader.GetDateTime(reader.GetOrdinal("updated_at"))


						};
					}
				}

			}

		}

        public Utilizator GetUserByEmailsauCNP(string emailsaucnp)
        {
            using (SqlConnection conn = _db.GetConnection())
            {
                if (conn.State == System.Data.ConnectionState.Closed)
                    conn.Open();

                using (SqlCommand cmd = new SqlCommand("sp_Utilizator_getbyEmailsauCNP", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@emailsaucnp", emailsaucnp);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        return new Utilizator
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Nume = reader.GetString(reader.GetOrdinal("nume")),
                            Prenume = reader.GetString(reader.GetOrdinal("prenume")),
                            Email = reader.GetString(reader.GetOrdinal("email")),
                            Cnp = reader.GetString(reader.GetOrdinal("cnp")),
                            RolId = reader.IsDBNull(reader.GetOrdinal("rol_id"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("rol_id")),
                            CompanieId = reader.IsDBNull(reader.GetOrdinal("companie_id"))
                                ? 0
                                : reader.GetInt32(reader.GetOrdinal("companie_id")),

                            ContVerificat = !reader.IsDBNull(reader.GetOrdinal("cont_verificat"))
                                && reader.GetBoolean(reader.GetOrdinal("cont_verificat")),

                            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
                                ? DateTime.Now
                                : reader.GetDateTime(reader.GetOrdinal("created_at")),

                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("updated_at"))


                        };
                    }
                }

            }

        }
    }
}
