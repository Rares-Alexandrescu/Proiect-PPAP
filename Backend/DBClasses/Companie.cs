namespace Backend.DBClasses
{
    public class Companie
    {
        public int ComapanieId { get; set; } = -1;
        public string Email { get; set; } = string.Empty;
        public string CnpAdminLocal { get; set; } = string.Empty;
        public string NumeAdminLocal { get; set;} = string.Empty;
        public string PrenumeAdminLocal { get; set;} = string.Empty;
        public string NumeCompanie { get; set; } = string.Empty;
        public string NumarTelefon { get; set; } = string.Empty;
    }
}