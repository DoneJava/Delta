namespace DELTAAPI.DTOs
{
    public class FreteCalculoDto
    {
        public string? Cep { get; set; }
        public string? Cupom { get; set; }
        public List<ProdutoFreteDto> Produtos { get; set; } = new();
        public Guid? Token { get; set; }
    }

    public class ProdutoFreteDto
    {
        public int IdProduto { get; set; }
        public int Quantidade { get; set; }
        public string Tamanho { get; set; } = "-";
    }
}
