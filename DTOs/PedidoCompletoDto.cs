using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DELTAAPI.DTOs
{
    [Keyless]
    public class PedidoCompletoDto
    {
        public int PedidoID { get; set; }
        public DateTime? DataPedido { get; set; }   // datetime -> DateTime?
        public string Status { get; set; } = "";
        public decimal ValorFrete { get; set; }
        public decimal ValorTotal { get; set; }

        public string MetodoEnvio { get; set; } = "";
        public string StatusEnvio { get; set; } = "";
        public string CodigoRastreamento { get; set; } = "";
        public DateTime? DataEnvio { get; set; }

        public int MetodoPagamento { get; set; }        // INT!
        public int StatusPagamento { get; set; }        // INT!
        public decimal ValorPago { get; set; }
        public DateTime? DataPagamento { get; set; }

        public string ItensJson { get; set; } = "";
        [NotMapped] public List<ItemDto> Itens { get; set; } = new();
    }
}
