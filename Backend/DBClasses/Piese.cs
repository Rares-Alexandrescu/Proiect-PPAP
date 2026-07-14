namespace Backend.DBClasses
{
	public class Piese
	{
		public int Piese_Id { get; set; } = -1;
		public int Furnizor_Id { get; set; } = -1;
		public decimal Pret_Cumparare { get; set; } = 0;
		public decimal Pret_Vanzare { get; set; } = 0;
		public string Nume_Piesa { get; set; } = string.Empty;
	}
}