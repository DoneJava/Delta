using System.ComponentModel.DataAnnotations;

namespace DELTAAPI.Enums
{
    public enum MetodoPagamento
    {
        [Display(Name = "Cartão de Crédito")]
        CartaoCredito = 1,

        [Display(Name = "Cartão de Débito")]
        CartaoDebito = 2,

        [Display(Name = "Pix")]
        Pix = 3,

        [Display(Name = "Boleto")]
        Boleto = 4
    }
    public enum StatusPagamento
    {
        [Display(Name = "Aguardando pagamento")]
        Aguardando = 1,

        [Display(Name = "Pago")]
        Pago = 2,

        [Display(Name = "Recusado")]
        Recusado = 3,

        [Display(Name = "Estornado")]
        Estornado = 4
    }
}
