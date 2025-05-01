using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DELTAAPI.Models
{
    public class Cliente
    {
        [JsonIgnore]
        public int ClienteID { get; set; }
        public string? Nome { get; set; }
        public string? CPF_CNPJ { get; set; }
        public string? Email { get; set; }
        public byte[]? Senha { get; set; }  // Armazenando a senha criptografada
        public string? Telefone { get; set; }

        // 🔁 NOVOS CAMPOS
        public string? Endereco { get; set; }           // Rua, número, etc.
        public string? Complemento { get; set; }        // Apto, bloco, etc.
        public string? CEP { get; set; }
        public bool? Portaria24Horas { get; set; }

        public DateTime? DataCadastro { get; set; } = DateTime.Now;

        [NotMapped]
        public string? SenhaEmTexto { get; set; }

        public Guid? Token { get; set; }
        public DateTime? ValidadeToken { get; set; }
    }
}
