namespace DELTAAPI.Model
{
    public sealed class Associado
    {
        public int AssociadoId { get; set; }
        public string Nome { get; set; } = "";
        public string? Documento { get; set; }
        public string Codigo { get; set; } = "";
        public bool Ativo { get; set; }
        public DateTime CriadoEmUtc { get; set; }
    }
}
