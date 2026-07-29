namespace Backend.DBClasses
{
    public class FacturiFurnizorLinie
    {
        public int facturi_linie_id { get; set; } = -1;
        public int comanda_piese_id{ get; set; } = -1;
        public int facturi_id { get; set; } = -1;

        public decimal pret_brut { get; set; } = 0m;

        public bool? stadiu_logistica { get; set; } = false;
    }
}