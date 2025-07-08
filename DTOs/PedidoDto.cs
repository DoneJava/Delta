using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Importante para [NotMapped]

namespace DELTAAPI.DTOs
{
    public class PedidoDto
    {
        [Key]
        public int PedidoID { get; set; }
        public int ClienteID { get; set; }
        public string NomeCliente { get; set; }
        public DateTime DataPedido { get; set; }
        public string Status { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal ValorFrete { get; set; }

        [NotMapped]
        public List<ItemDto> Itens { get; set; } = new List<ItemDto>();  // Inicialize para evitar null
    }

    public class ItemDto
    {
        public string Nome { get; set; }
        public string Tamanho { get; set; }
        public int Quantidade { get; set; }
        public decimal Preco { get; set; }
        public string ImagemUrl { get; set; }
    }
}
