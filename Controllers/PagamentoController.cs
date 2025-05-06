#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Enums;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public PagamentoController(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<PagamentoDto>>> ObterTodosPagamentos()
        {
            try
            {
                List<Pagamento> pagamentosRaw = await _context.Pagamentos
                    .FromSqlRaw("EXEC ListarPagamentos")
                    .AsNoTracking()
                    .ToListAsync();

                List<PagamentoDto> pagamentos = pagamentosRaw
                    .Select(p => new PagamentoDto
                    {
                        PagamentoID = p.PagamentoID,
                        PedidoID = p.PedidoID,
                        ValorPago = p.ValorPago,
                        MetodoPagamento = p.MetodoPagamento,
                        StatusPagamento = p.StatusPagamento,
                        DataPagamento = p.DataPagamento
                    })
                    .ToList();

                return Ok(pagamentos);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<ActionResult<PagamentoDto>> ObterPagamentoPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@PagamentoID", id);

                Pagamento? pagamento = await Task.Run(() =>
                    _context.Pagamentos
                        .FromSqlRaw("EXEC ObterPagamentoPorID @PagamentoID", param)
                        .AsNoTracking()
                        .AsEnumerable()
                        .FirstOrDefault());

                if (pagamento == null)
                    return NotFound("Pagamento não encontrado.");

                PagamentoDto dto = new PagamentoDto
                {
                    PagamentoID = pagamento.PagamentoID,
                    PedidoID = pagamento.PedidoID,
                    ValorPago = pagamento.ValorPago,
                    MetodoPagamento = pagamento.MetodoPagamento,
                    StatusPagamento = pagamento.StatusPagamento,
                    DataPagamento = pagamento.DataPagamento
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
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarPagamento([FromBody] PagamentoCreateDto dto)
        {
            try
            {
                if (!Enum.IsDefined(typeof(MetodoPagamento), dto.MetodoPagamento))
                    return BadRequest("Método de pagamento inválido.");

                Pedido? pedido = await _context.Pedidos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PedidoID == dto.PedidoID);

                if (pedido == null)
                    return NotFound("Pedido não encontrado.");

                if (dto.ValorPago != pedido.ValorTotal)
                    return BadRequest("O valor pago deve ser igual ao valor total do pedido.");

                bool pagamentoJaExiste = await _context.Pagamentos
                    .AsNoTracking()
                    .AnyAsync(p => p.PedidoID == dto.PedidoID && p.StatusPagamento != StatusPagamento.Estornado);

                if (pagamentoJaExiste)
                    return Conflict("Este pedido já possui um pagamento ativo registrado.");

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirPagamento @PedidoID, @ValorPago, @MetodoPagamento, @StatusPagamento, @DataPagamento",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ValorPago", dto.ValorPago),
                    new SqlParameter("@MetodoPagamento", dto.MetodoPagamento),
                    new SqlParameter("@StatusPagamento", StatusPagamento.Aguardando),
                    new SqlParameter("@DataPagamento", DateTime.Now)
                );

                return Ok("Pagamento registrado com sucesso e aguardando processamento.");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Erro ao registrar pagamento: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST - Validar Cupom
        [HttpPost("validarCupom")]
        public async Task<IActionResult> ValidarCupom([FromBody] CupomDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Codigo))
                    return BadRequest("Código do cupom não pode ser vazio.");

                SqlParameter paramCodigo = new SqlParameter("@Codigo", dto.Codigo);

                SqlParameter paramValido = new SqlParameter
                {
                    ParameterName = "@Valido",
                    SqlDbType = System.Data.SqlDbType.Bit,
                    Direction = System.Data.ParameterDirection.Output
                };

                SqlParameter paramPorcentagem = new SqlParameter
                {
                    ParameterName = "@DescontoPorcentagem",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output
                };

                SqlParameter paramValor = new SqlParameter
                {
                    ParameterName = "@DescontoValor",
                    SqlDbType = System.Data.SqlDbType.Decimal,
                    Precision = 18,
                    Scale = 2,
                    Direction = System.Data.ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ValidarCupom @Codigo, @Valido OUTPUT, @DescontoPorcentagem OUTPUT, @DescontoValor OUTPUT",
                    paramCodigo, paramValido, paramPorcentagem, paramValor
                );

                CupomResultadoDto resultado = new CupomResultadoDto
                {
                    Valido = (bool)(paramValido.Value ?? false),
                    DescontoPorcentagem = paramPorcentagem.Value != DBNull.Value ? (int)paramPorcentagem.Value : 0,
                    DescontoValor = paramValor.Value != DBNull.Value ? (decimal)paramValor.Value : 0
                };

                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Erro ao validar cupom: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion


        #region POST Estornar
        [HttpPost("estornar/{id}")]
        public async Task<IActionResult> EstornarPagamento(int id)
        {
            try
            {
                Pagamento? pagamento = await _context.Pagamentos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PagamentoID == id);

                if (pagamento == null)
                    return NotFound("Pagamento não encontrado.");

                if (pagamento.StatusPagamento != StatusPagamento.Pago)
                    return BadRequest("Somente pagamentos com status 'Pago' podem ser estornados.");

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC EstornarPagamento @PagamentoID",
                    new SqlParameter("@PagamentoID", id)
                );

                return Ok("Pagamento estornado com sucesso.");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Erro ao estornar pagamento: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region PUT (bloqueado)
        [HttpPut("editar/{id}")]
        public IActionResult EditarPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser editados. Estorne ou crie um novo pagamento.");
        }
        #endregion

        #region DELETE (bloqueado)
        [HttpDelete("excluir/{id}")]
        public IActionResult ExcluirPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser deletados.");
        }
        #endregion
    }
}
