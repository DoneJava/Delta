namespace DELTAAPI.Model
{
    public class Cupom
    {
        public int CupomID { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int? DescontoPorcentagem { get; set; }
        public decimal? DescontoValor { get; set; }
        public DateTime? Validade { get; set; }
    }
}
