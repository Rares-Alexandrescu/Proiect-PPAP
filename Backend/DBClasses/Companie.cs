namespace Backend.DBClasses
{
    public class Companie
    {
        public int Companie_Id { get; set; } = -1;
        public string Email { get; set; } = string.Empty;
        public string CnpAdminLocal { get; set; } = string.Empty;
        public string NumeAdminLocal { get; set;} = string.Empty;
        public string PrenumeAdminLocal { get; set;} = string.Empty;
        public string Nume_Companie { get; set; } = string.Empty;
        public string Numar_Telefon { get; set; } = string.Empty;
    }
}