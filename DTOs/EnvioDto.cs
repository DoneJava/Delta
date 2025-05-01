namespace DELTAAPI.DTOs
{
    public class EnvioDto
    {
        public int EnvioID { get; set; }
        public int PedidoID { get; set; }
        public string MetodoEnvio { get; set; }
        public string StatusEnvio { get; set; }
        public string CodigoRastreamento { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}
