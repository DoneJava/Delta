using DELTAAPI.Enums;
using DELTAAPI.Helpers;

namespace DELTAAPI.DTOs
{
    public class PagamentoDto
    {
        public int PagamentoID { get; set; }
        public int PedidoID { get; set; }
        public decimal ValorPago { get; set; }

        public MetodoPagamento MetodoPagamento { get; set; }
        public string MetodoPagamentoNome => MetodoPagamento.GetDisplayName();

        public StatusPagamento StatusPagamento { get; set; }
        public string StatusPagamentoNome => StatusPagamento.GetDisplayName();

        public DateTime DataPagamento { get; set; }
    }
}
