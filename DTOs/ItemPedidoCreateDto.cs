namespace DELTAAPI.DTOs
{
    public class ItemPedidoCreateDto
    {
        public int PedidoID { get; set; }
        public int ProdutoID { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}
