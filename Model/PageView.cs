namespace DELTAAPI.Model
{
    public class PageView
    {
        public int PageViewId { get; set; }
        public DateTime CriadoEmBrt { get; set; } // BR direto
        public string Route { get; set; } = default!;
        public string? Url { get; set; }
        public Guid? AnonId { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
    }
}
