namespace DELTAAPI.DTOs
{
    public class PagamentoProcessarDto
    {
        public List<ProdutoCompraDto> Produtos { get; set; } = new();
        public int MetodoPagamento { get; set; }
        public string DadosPagamento { get; set; } = string.Empty;
        public string? DadosEnvio { get; set; }
        public string? Cupom { get; set; }
        public string? Cep { get; set; }
    }

    public class ProdutoCompraDto
    {
        public int IdProduto { get; set; }
        public int Quantidade { get; set; }
        public string Tamanho { get; set; } = "-";
    }
}
