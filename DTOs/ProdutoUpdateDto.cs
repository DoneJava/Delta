namespace DELTAAPI.DTOs
{
    public class ProdutoUpdateDto
    {
        public int ProdutoID { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public DateTime DataCadastro { get; set; }
        public int CategoriaID { get; set; }
        public string CategoriaNome { get; set; }
        public string ImagemPrincipal { get; set; }
    }
}
