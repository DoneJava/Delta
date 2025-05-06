using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DELTAAPI.Models
{
    public class Contato
    {
        [Key]
        public int ContatoId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Assunto { get; set; } = string.Empty;

        [Required]
        public string Mensagem { get; set; } = string.Empty;

        public DateTime DataEnvio { get; set; } = DateTime.Now;

        public int? ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        public int? PedidoId { get; set; }
        [ForeignKey("PedidoId")]
        public Pedido? Pedido { get; set; }
    }
}
