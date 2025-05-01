namespace DELTAAPI.DTOs
{
    public class ItemPedidoUpdateDto
    {
        public int ItemPedidoID { get; set; }
        public int PedidoID { get; set; }
        public int ProdutoID { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
