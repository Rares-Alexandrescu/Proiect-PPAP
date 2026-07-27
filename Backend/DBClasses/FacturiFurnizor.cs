namespace Backend.DBClasses
{
    public class FacturiFurnizor
    {
        public int facturi_id { get; set; } = -1;
        public int furnizor_id { get; set; } = -1;

        public decimal pret_total_brut { get; set; } = 0m;

        public string? path_factura_pdf { get; set; } = string.Empty;
        public bool? stadiu_plata { get; set; } = false;

        public DateTime created_at { get; set; } = DateTime.Now;
    }
}