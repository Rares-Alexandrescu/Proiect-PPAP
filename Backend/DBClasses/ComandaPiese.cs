namespace Backend.DBClasses
{
    public class ComandaPiese
    {
        public int comanda_piese_id { get; set; } = -1;
        public int comanda_id { get; set; } = -1;
        public int piese_id { get; set; } = -1;
        public int cantitate_comandata { get; set; } = -1;
        public string? detalii_piese { get; set; } = string.Empty;
    }
}