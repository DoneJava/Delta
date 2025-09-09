using DELTAAPI.DTOs;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersiculosController : ControllerBase
    {
        private readonly VersiculosService _service;

        public VersiculosController(VersiculosService service)
        {
            _service = service;
        }

        // GET api/versiculos/por-referencia?ref=Jo%203:16-18,21
        [HttpGet("por-referencia")]
        public async Task<ActionResult<List<VersoDto>>> PorReferencia([FromQuery] string? @ref, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(@ref)) return BadRequest("Informe a referência.");
            var data = await _service.BuscarPorReferenciaAsync(@ref!, ct);
            return Ok(data);
        }

        // GET api/versiculos/buscar?livro=Joao&capitulo=3&versiculo=16-18,21
        [HttpGet("buscar")]
        public async Task<ActionResult<List<VersoDto>>> Buscar(
            [FromQuery] string? livro,
            [FromQuery] int? capitulo,
            [FromQuery(Name = "versiculo")] string? versoExpr,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(livro) && !capitulo.HasValue)
                return BadRequest("Informe ao menos o livro ou a referência.");

            var data = await _service.BuscarAsync(livro ?? "", capitulo, versoExpr, ct);
            return Ok(data);
        }

        // (Opcional) GET api/versiculos/livros
        [HttpGet("livros")]
        public async Task<ActionResult<List<string>>> Livros(CancellationToken ct)
        {
            var livros = await _service.ListarLivrosAsync(ct);
            return Ok(livros);
        }
    }
}
