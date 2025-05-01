namespace DELTAAPI.DTOs
{
    public class PedidoUpdateDto
    {
        public int PedidoID { get; set; }
        public int ClienteID { get; set; }
        public string Status { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
