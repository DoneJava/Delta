namespace DELTAAPI.DTOs
{
    public class PixPagamentoDto
    {
        public decimal Valor { get; set; }
        public string? Email { get; set; } // opcional, mas recomendado para identificação no Mercado Pago
    }
}
