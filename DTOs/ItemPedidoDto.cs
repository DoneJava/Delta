namespace DELTAAPI.DTOs
{
    public class ItemPedidoDto
    {
        public int ItemPedidoID { get; set; }
        public int PedidoID { get; set; }
        public int ProdutoID { get; set; }
        public string NomeProduto { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal => Quantidade * PrecoUnitario;
    }
}
