namespace DELTAAPI.DTOs
{
    public class PedidoClienteDto
    {
        public int PedidoID { get; set; }
        public int ClienteID { get; set; }
        public string NomeCliente { get; set; }
        public DateTime DataPedido { get; set; }
        public string Status { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
