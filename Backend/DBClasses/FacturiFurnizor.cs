namespace Backend.DBClasses
{
    public class FacturiFurnizor
    {
        public int facturi_id { get; set; } = -1;
        public int furnizor_id { get; set; } = -1;
        public int comanda_piese_id { get; set; } = -1;

        public decimal pret_brut { get; set; } = 0m;

        public bool? stadiu_plata { get; set; } = false;
        public bool? stadiu_logistica { get; set: } = false;

        public string? path_factura_furnizor { get; set; } = string.Empty;

        public DateTime created_at { get; set; } = DateTime.Now;
    }
}