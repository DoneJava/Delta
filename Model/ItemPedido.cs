namespace DELTAAPI.Models
{
    public class ItemPedido
    {
        public int ItemPedidoID { get; set; }
        public int PedidoID { get; set; }  // Pedido ao qual o item pertence
        public int ProdutoID { get; set; }  // Produto que foi comprado
        public int Quantidade { get; set; }  // Quantidade do produto
        public decimal PrecoUnitario { get; set; }  // Preço do produto no momento da compra

        public Pedido Pedido { get; set; }  // Relacionamento com Pedido
        public Produto Produto { get; set; }  // Relacionamento com Produto
    }
}
