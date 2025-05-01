namespace DELTAAPI.DTOs
{
    public class PedidoCreateDto
    {
        public int ClienteID { get; set; }
        public string Status { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
