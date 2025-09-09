namespace DELTAAPI.DTOs
{
    public class PageViewDto
    {
        public string? Route { get; set; }   // "produto-detalhes?id=123"
        public string? Url { get; set; }     // location.href, opcional
        public string? AnonId { get; set; }  // opcional
    }
}
