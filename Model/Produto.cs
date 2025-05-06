public class Produto
{
    public int ProdutoID { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string? ImagemPrincipal { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public string? Categorias { get; set; }
    public string? TamanhosDisponiveis { get; set; }
    public bool? Destaque { get; set; }
}
