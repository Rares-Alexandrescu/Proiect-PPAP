namespace Backend.DBClasses
{
    public class Utilizator
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nume { get; set; } = string.Empty;
        public string Prenume { get; set; } = string.Empty;
        public string Parola { get; set; } = string.Empty;
        public string Cnp { get; set; } = string.Empty;
        public int? rol_id { get; set; } = int.MaxValue;
        public int? companie_id { get; set; } = int.MaxValue;
        public DateTime created_at { get; set; } 
        public bool cont_verificat { get; set; } = false;
        public DateTime updated_at { get; set; }
        public string? JWT; //ASTA NU E IN BAZA DE DATE
    }
}