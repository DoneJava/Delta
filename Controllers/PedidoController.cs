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
    public class PedidoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public PedidoController(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<PedidoDto>>> ObterTodosPedidos()
        {
            try
            {
                List<Pedido> pedidosRaw = await _context.Pedidos
                    .FromSqlRaw("EXEC ListarPedidos")
                    .AsNoTracking()
                    .ToListAsync();

                List<Cliente> clientes = await _context.Clientes
                    .AsNoTracking()
                    .ToListAsync();

                List<PedidoDto> pedidos = pedidosRaw
                    .Join(clientes,
                        pedido => pedido.ClienteID,
                        cliente => cliente.ClienteID,
                        (pedido, cliente) => new PedidoDto
                        {
                            PedidoID = pedido.PedidoID,
                            ClienteID = pedido.ClienteID,
                            NomeCliente = cliente.Nome,
                            DataPedido = pedido.DataPedido,
                            Status = pedido.Status,
                            ValorTotal = pedido.ValorTotal
                        })
                    .ToList();

                return Ok(pedidos);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<ActionResult<PedidoDto>> ObterPedidoPorId(int id)
        {
            try
            {
                SqlParameter parametro = new SqlParameter("@PedidoID", id);

                List<Pedido> pedidos = await _context.Pedidos
                    .FromSqlRaw("EXEC ObterPedidoPorID @PedidoID", parametro)
                    .AsNoTracking()
                    .ToListAsync();

                Pedido? pedido = pedidos.FirstOrDefault();

                if (pedido == null)
                    return NotFound("Pedido não encontrado.");

                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClienteID == pedido.ClienteID);

                PedidoDto dto = new PedidoDto
                {
                    PedidoID = pedido.PedidoID,
                    ClienteID = pedido.ClienteID,
                    NomeCliente = cliente?.Nome ?? string.Empty,
                    DataPedido = pedido.DataPedido,
                    Status = pedido.Status,
                    ValorTotal = pedido.ValorTotal
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
        public async Task<IActionResult> CriarPedido([FromBody] PedidoCreateDto dto)
        {
            try
            {
                Boolean clienteExiste = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.ClienteID == dto.ClienteID);

                if (!clienteExiste)
                    return NotFound("Cliente informado não existe.");

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirPedido @ClienteID, @Status, @ValorTotal",
                    new SqlParameter("@ClienteID", dto.ClienteID),
                    new SqlParameter("@Status", dto.Status),
                    new SqlParameter("@ValorTotal", dto.ValorTotal)
                );

                return Ok("Pedido criado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region PUT
        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarPedido(int id, [FromBody] PedidoUpdateDto dto)
        {
            if (id != dto.PedidoID)
                return BadRequest("ID inconsistente.");

            try
            {
                Boolean clienteExiste = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.ClienteID == dto.ClienteID);

                if (!clienteExiste)
                    return NotFound("Cliente informado não existe.");

                Int32 linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AtualizarPedido @PedidoID, @ClienteID, @Status, @ValorTotal",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ClienteID", dto.ClienteID),
                    new SqlParameter("@Status", dto.Status),
                    new SqlParameter("@ValorTotal", dto.ValorTotal)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Pedido não encontrado para atualização.");

                return Ok("Pedido atualizado com sucesso.");
            }
            catch (SqlException excecao) when (excecao.Number == 50000 || excecao.Number == 547)
            {
                return BadRequest("Erro de integridade: " + excecao.Message);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> ExcluirPedido(int id)
        {
            try
            {
                Int32 linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarPedido @PedidoID",
                    new SqlParameter("@PedidoID", id)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Pedido não encontrado para exclusão.");

                return Ok("Pedido deletado com sucesso.");
            }
            catch (SqlException excecao) when (excecao.Number == 547)
            {
                return BadRequest("Não é possível excluir o pedido: ele está relacionado a outros dados.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion
    }
}
