namespace DELTAAPI.DTOs
{
    public class ProdutoCreateDto
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public int Estoque { get; set; }
        public byte[] ImagemPrincipal { get; set; }
        public int CategoriaID { get; set; }
    }
}
