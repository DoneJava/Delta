namespace DELTAAPI.Models
{
    public class Envio
    {
        public int EnvioID { get; set; }
        public int PedidoID { get; set; }
        public string MetodoEnvio { get; set; }
        public string StatusEnvio { get; set; }
        public string CodigoRastreamento { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.Now;
        public Pedido Pedido { get; set; }
    }
}
