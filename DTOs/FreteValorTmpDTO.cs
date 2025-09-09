using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.DTOs
{
    [Keyless]
    public class FreteValorTmpDTO
    {
        public decimal Valor { get; set; }
        public int? Prazo { get; set; }
    }
}
