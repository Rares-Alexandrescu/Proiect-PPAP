namespace Backend.DBClasses
{
    public class Comanda
    {
        public int comanda_id { get; set; } = -1;
        public int? documente_id { get; set; } = -1;
        public bool stadiu_finalizare { get; set; } = false;
        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}