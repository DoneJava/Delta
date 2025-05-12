namespace DELTAAPI.DTOs
{
    public class DadosCalculoValorDto
    {
        public List<DadosProdutoDto>? Produtos { get; set; }
        public string? Cupom { get; set; }
    }
    public class DadosProdutoDto
    {
        public int IdProduto { get; set; }
        public string? Tamanho { get; set; }
        public int Quantidade { get; set; }
    }
    public class ValorPagamentoDto
    {
        public decimal? valorTotal { get; set; }
    }
}