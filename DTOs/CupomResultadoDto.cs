namespace DELTAAPI.DTOs
{
    public class CupomResultadoDto
    {
        public bool Valido { get; set; }
        public int DescontoPorcentagem { get; set; }
        public decimal DescontoValor { get; set; }
        public bool? FreteGratis { get; set; }
    }
}
