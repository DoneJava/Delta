namespace DELTAAPI.Models
{
    public class ImagemProduto
    {
        public int ImagemID { get; set; }
        public int ProdutoID { get; set; }
        public string Imagem { get; set; }  // Agora é caminho (ex: produto-1-1.png)
        public bool ImagemPrincipal { get; set; }

        public Produto Produto { get; set; }
    }
}
