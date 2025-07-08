namespace DELTAAPI.Models
{
    public class Pedido
    {
        public int PedidoID { get; set; }
        public int ClienteID { get; set; }
        public DateTime DataPedido { get; set; } = DateTime.Now;
        public string Status { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal ValorFrete { get; set; }
        public Cliente Cliente { get; set; }
    }
}
