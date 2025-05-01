namespace DELTAAPI.DTOs
{
    public class ClienteUpdateDto
    {
        public int ClienteID { get; set; }
        public string Nome { get; set; }
        public string CPF_CNPJ { get; set; }
        public string Email { get; set; }

        public string SenhaAtual { get; set; }
        public string SenhaEmTexto { get; set; }

        public string? Telefone { get; set; }
        public string? Endereco { get; set; }
        public string? Complemento { get; set; }
        public string? CEP { get; set; }
        public bool Portaria24Horas { get; set; }
    }
}
