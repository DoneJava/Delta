namespace DELTAAPI.DTOs
{
    public class CardPayDto
    {
        public List<DadosProdutoDto> Produtos { get; set; } = new();
        public string? Cupom { get; set; }
        public string? Cep { get; set; }
        public string? DadosEnvio { get; set; } // JSON vindo do front
        public CardDataDto Card { get; set; } = new();
    }

    public class CardDataDto
    {
        public string Token { get; set; } = "";                // cardFormData.token
        public int Installments { get; set; } = 1;             // cardFormData.installments
        public string? PaymentMethodId { get; set; }           // cardFormData.paymentMethodId
        public string? IssuerId { get; set; }                  // cardFormData.issuerId
        public CardPayerDto Payer { get; set; } = new();
    }

    public class CardPayerDto
    {
        public string? Email { get; set; }
        public string? IdentificationType { get; set; }   // e.g. "CPF"
        public string? IdentificationNumber { get; set; } // e.g. "12345678901"
    }

}
