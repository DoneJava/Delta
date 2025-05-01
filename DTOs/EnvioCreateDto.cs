namespace DELTAAPI.DTOs
{
    public class EnvioCreateDto
    {
        public int PedidoID { get; set; }
        public string MetodoEnvio { get; set; }
        public string StatusEnvio { get; set; }
        public string CodigoRastreamento { get; set; }
    }
}
