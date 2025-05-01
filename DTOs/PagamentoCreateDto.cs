using DELTAAPI.Enums;

namespace DELTAAPI.DTOs
{
    public class PagamentoCreateDto
    {
        public int PedidoID { get; set; }
        public decimal ValorPago { get; set; }
        public MetodoPagamento MetodoPagamento { get; set; }
        public StatusPagamento StatusPagamento { get; set; }
    }
}
