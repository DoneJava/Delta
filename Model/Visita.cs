namespace DELTAAPI.Models
{
    public class Visita
    {
        public long VisitaId { get; set; }   // PK (IDENTITY)
        public DateTime Dia { get; set; }   // só a data (UTC no servidor)
        public Guid? AnonId { get; set; }   // id anônimo do device
        public string? Url { get; set; }
        public string? Referrer { get; set; }
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? Ip { get; set; }   // IPv4/IPv6
        public string? UserAgent { get; set; }
        public DateTime CreatedAtUtc { get; set; }   // default SYSUTCDATETIME()
    }
}
