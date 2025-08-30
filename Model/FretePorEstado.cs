namespace DELTAAPI.Model
{
    public class FretePorEstado
    {
        public int Id { get; set; }
        public string UF { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Prazo { get; set; }
        public string CepInicial { get; set; } = string.Empty;
        public string CepFinal { get; set; } = string.Empty;
    }
}
