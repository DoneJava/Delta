#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentoController : ControllerBase
    {
        #region Fields
        private PagamentoService _PagamentoService;
        #endregion

        #region Constructor
        public PagamentoController(PagamentoService pagamentoService)
        {
            _PagamentoService = pagamentoService;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosPagamentos()
        {
            RetornoDTO retornoDTO = await _PagamentoService.ObterTodosPagamentos();

            if (!retornoDTO.Sucesso)
            {
                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }

            return StatusCode((int)retornoDTO.Status, retornoDTO.Objeto);
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<IActionResult> ObterPagamentoPorId(int id)
        {
            RetornoDTO retornoDTO = await _PagamentoService.ObterPagamentoPorId(id);

            if (!retornoDTO.Sucesso)
            {
                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }

            return StatusCode((int)retornoDTO.Status, retornoDTO.Objeto);
        }
        #endregion

        #region POST - Validar Cupom
        [HttpPost("validarCupom")]
        public async Task<IActionResult> ValidarCupom([FromBody] CupomDto dto)
        {
            RetornoDTO retornoDTO = await _PagamentoService.ValidarCupom(dto.Codigo);

            if (!retornoDTO.Sucesso)
            {
                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }

            return StatusCode((int)retornoDTO.Status, retornoDTO.Objeto);
        }
        #endregion

        #region Calcular valor total pagamento
        [HttpPost("calcular_valor_total")]
        public async Task<IActionResult> CalcularValorTotal([FromBody] DadosCalculoValorDto dto)
        {
            string? tokenStr = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            var retornoDTO = await _PagamentoService.CalcularValorTotal(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto : new { mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region Calcular Frete
        [HttpPost("calcular-frete")]
        public async Task<IActionResult> CalcularFrete([FromBody] FreteCalculoDto dto)
        {
            try
            {

                List<FreteResultadoDto> resultados = new List<FreteResultadoDto>
                {
                    new FreteResultadoDto
                    {
                        Transportadora = "Desconhecida",
                        Valor = 10,
                        PrazoEntrega = 10
                    }
                };

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro interno ao calcular o frete.");
            }
        }
        #endregion

        #region Processar Pagamento
        [HttpPost("processar_pagamento")]
        public async Task<IActionResult> ProcessarPagamento([FromBody] PagamentoProcessarDto dto)
        {
            string? tokenStr = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            var retornoDTO = await _PagamentoService.ProcessarPagamento(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region POST Estornar
        [HttpPost("estornar/{id}")]
        public async Task<IActionResult> EstornarPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser Estornados.");
            //try
            //{
            //    Pagamento? pagamento = await _context.Pagamentos
            //        .AsNoTracking()
            //        .FirstOrDefaultAsync(p => p.PagamentoID == id);

            //    if (pagamento == null)
            //        return NotFound("Pagamento não encontrado.");

            //    if (pagamento.StatusPagamento != StatusPagamento.Pago)
            //        return BadRequest("Somente pagamentos com status 'Pago' podem ser estornados.");

            //    await _context.Database.ExecuteSqlRawAsync(
            //        "EXEC EstornarPagamento @PagamentoID",
            //        new SqlParameter("@PagamentoID", id)
            //    );

            //    return Ok("Pagamento estornado com sucesso.");
            //}
            //catch (SqlException ex)
            //{
            //    return StatusCode(500, $"Erro ao estornar pagamento: {ex.Message}");
            //}
            //catch
            //{
            //    return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            //}
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

        #region PIX
        [HttpPost("gerar-pix")]
        public async Task<IActionResult> GerarPagamentoPix([FromBody] DadosCalculoValorDto dto)
        {
            string? tokenStr = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            var retornoDTO = await _PagamentoService.GerarPagamentoPix(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }

        [HttpGet("status-pix")]
        public async Task<IActionResult> ConsultarStatusPix([FromQuery] string id)
        {
            var retornoDTO = await _PagamentoService.ConsultarStatusPix(id);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion
    }
}
