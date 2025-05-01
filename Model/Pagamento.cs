using DELTAAPI.Enums;

namespace DELTAAPI.Models
{
    public class Pagamento
    {
        public int PagamentoID { get; set; }
        public int PedidoID { get; set; }
        public decimal ValorPago { get; set; }

        public MetodoPagamento MetodoPagamento { get; set; }  // Agora é enum
        public StatusPagamento StatusPagamento { get; set; }  // Agora é enum

        public DateTime DataPagamento { get; set; } = DateTime.Now;
        public Pedido Pedido { get; set; }
    }
}
