namespace DELTAAPI.DTOs
{
    public class ProdutoDetalhesDto
    {
        public int ProdutoID { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public DateTime DataCadastro { get; set; }
        public string? ImagemUrl { get; set; }
        public List<string>? TamanhosDisponiveis { get; set; }
        public string ? Categorias { get; set; }
        public long QtdVisualizacao { get; set; }
    }
}

