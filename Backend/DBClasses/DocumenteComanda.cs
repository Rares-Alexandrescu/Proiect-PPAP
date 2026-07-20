namespace Backend.DBClasses
{
    public class DocumenteComanda
    {
        public int comanda_id { get; set; } = -1;
        public int documente_id { get; set; } = -1;

        public bool? stadiu_acceptare { get; set; }
        public string? path_documente_pdf { get; set; }

        public DateTime created_at { get; set; } = DateTime.UtcNow;
    }
}