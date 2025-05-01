#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnvioController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public EnvioController(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<EnvioDto>>> ObterTodosEnvios()
        {
            try
            {
                List<Envio> enviosRaw = await _context.Envios
                    .FromSqlRaw("EXEC ListarEnvios")
                    .AsNoTracking()
                    .ToListAsync();

                List<Pedido> pedidos = await _context.Pedidos
                    .AsNoTracking()
                    .ToListAsync();

                List<EnvioDto> envios = enviosRaw
                    .Join(pedidos,
                        e => e.PedidoID,
                        p => p.PedidoID,
                        (e, p) => new EnvioDto
                        {
                            EnvioID = e.EnvioID,
                            PedidoID = e.PedidoID,
                            MetodoEnvio = e.MetodoEnvio,
                            StatusEnvio = e.StatusEnvio,
                            CodigoRastreamento = e.CodigoRastreamento,
                            DataEnvio = e.DataEnvio
                        })
                    .ToList();

                return Ok(envios);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<ActionResult<EnvioDto>> ObterEnvioPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@EnvioID", id);

                List<Envio> envios = await _context.Envios
                    .FromSqlRaw("EXEC ObterEnvioPorID @EnvioID", param)
                    .AsNoTracking()
                    .ToListAsync();

                Envio? envio = envios.FirstOrDefault();

                if (envio == null)
                    return NotFound("Envio não encontrado.");

                EnvioDto dto = new EnvioDto
                {
                    EnvioID = envio.EnvioID,
                    PedidoID = envio.PedidoID,
                    MetodoEnvio = envio.MetodoEnvio,
                    StatusEnvio = envio.StatusEnvio,
                    CodigoRastreamento = envio.CodigoRastreamento,
                    DataEnvio = envio.DataEnvio
                };

                return Ok(dto);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST
        [HttpPost("criar")]
        public async Task<IActionResult> CriarEnvio([FromBody] EnvioCreateDto dto)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirEnvio @PedidoID, @MetodoEnvio, @StatusEnvio, @CodigoRastreamento",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@MetodoEnvio", dto.MetodoEnvio),
                    new SqlParameter("@StatusEnvio", dto.StatusEnvio),
                    new SqlParameter("@CodigoRastreamento", dto.CodigoRastreamento)
                );

                return Ok("Envio criado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region PUT
        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarEnvio(int id, [FromBody] EnvioUpdateDto dto)
        {
            if (id != dto.EnvioID)
                return BadRequest("ID inconsistente.");

            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AtualizarEnvio @EnvioID, @PedidoID, @MetodoEnvio, @StatusEnvio, @CodigoRastreamento",
                    new SqlParameter("@EnvioID", dto.EnvioID),
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@MetodoEnvio", dto.MetodoEnvio),
                    new SqlParameter("@StatusEnvio", dto.StatusEnvio),
                    new SqlParameter("@CodigoRastreamento", dto.CodigoRastreamento)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Envio não encontrado para atualização.");

                return Ok("Envio atualizado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarEnvio(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarEnvio @EnvioID",
                    new SqlParameter("@EnvioID", id)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Envio não encontrado para exclusão.");

                return Ok("Envio deletado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion
    }
}