namespace DELTAAPI.Model
{
    public class Versiculos
    {
        public int VersiculoId { get; set; }
        public string Livro { get; set; } = "";
        public int Capitulo { get; set; }
        public int Versiculo { get; set; }
        public string Texto { get; set; } = "";
    }
}
