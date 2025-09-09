using Microsoft.EntityFrameworkCore;
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

    [Keyless]
    public class ItemDto
    {
        public int ItemPedidoID { get; set; }
        public int ProdutoID { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }   // <- tenha este nome
        public string Nome { get; set; } = "";
        public string ImagemPrincipal { get; set; } = "";
        public string? Tamanho { get; set; }
        public string? TamanhoSelecionado { get; set; }     // alias, também do I.Tamanho
        public string? TamanhosDisponiveis { get; set; }
    }


    [Keyless]
    public class ClienteIdDto
    {
        public int ClienteId { get; set; }
    }
}
