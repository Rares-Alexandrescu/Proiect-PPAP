using System;

namespace PPAP_Proiect.Models
{
	public class Utilizator
	{
		public int Id { get; set; }
		public string Nume { get; set; } = string.Empty;
		public string Prenume { get; set; } = string.Empty;
		public string Parola { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Cnp { get; set; } = string.Empty;
		public int? RolId { get; set; }
		public int? CompanieId { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime? UpdatedAt { get; set; }
		public bool ContVerificat { get; set; } = false;
	}
}
