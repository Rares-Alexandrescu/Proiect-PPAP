namespace Backend.DBClasses
{
    public class FacturaCompanie
    {
        public int factura_id { get; set; } = -1;
        public int comanda_id { get; set; } = -1;
        public int companie_id { get; set; } = -1;

        public decimal pret_brut { get; set; } = 0m;

        public string? path_factura_pdf { get; set; }


        public bool? stadiu_plata { get; set; } = false;

        public DateTime created_at { get; set; };
    }
}