#region Usings
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentoController : ControllerBase
    {
        #region Fields
        private readonly PagamentoService _pagamentoService;
        #endregion

        #region Constructor
        public PagamentoController(PagamentoService pagamentoService)
        {
            _pagamentoService = pagamentoService;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Extrai o token do cabeçalho Authorization replicando o comportamento:
        /// Request.Headers.Authorization.ToString().Replace("Bearer ", "")
        /// </summary>
        private string? GetBearerTokenOrNull()
        {
            // Mantém SEMPRE o mesmo comportamento do código original
            // (não valida prefixo, apenas remove "Bearer " se existir)
            return Request?.Headers.Authorization.ToString().Replace("Bearer ", "");
        }
        #endregion

        #region GET All
        /// <summary>
        /// Lista todos os pagamentos.
        /// </summary>
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosPagamentos()
        {
            RetornoDTO retornoDTO = await _pagamentoService.ObterTodosPagamentos();

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
        /// <summary>
        /// Obtém um pagamento pelo ID.
        /// </summary>
        [HttpGet("obter-por-id/{id}")]
        public async Task<IActionResult> ObterPagamentoPorId(int id)
        {
            RetornoDTO retornoDTO = await _pagamentoService.ObterPagamentoPorId(id);

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
        /// <summary>
        /// Valida um cupom informado.
        /// </summary>
        [HttpPost("validarCupom")]
        public async Task<IActionResult> ValidarCupom([FromBody] CupomDto dto)
        {
            RetornoDTO retornoDTO = await _pagamentoService.ValidarCupom(dto.Codigo);

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
        /// <summary>
        /// Calcula o valor total a pagar (itens, frete, cupom etc.).
        /// </summary>
        [HttpPost("calcular_valor_total")]
        public async Task<IActionResult> CalcularValorTotal([FromBody] DadosCalculoValorDto dto)
        {
            string? tokenStr = GetBearerTokenOrNull();

            var retornoDTO = await _pagamentoService.CalcularValorTotal(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto : new { mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region Calcular Frete
        /// <summary>
        /// Calcula o frete a partir dos dados informados.
        /// </summary>
        [HttpPost("calcular-frete")]
        public async Task<IActionResult> CalcularFrete([FromBody] FreteCalculoDto dto)
        {
            var retornoDTO = await _pagamentoService.CalcularFrete(dto);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto
                                   : new { mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region Processar Pagamento
        /// <summary>
        /// Processa o pagamento (cartão, débito, PIX, etc.).
        /// </summary>
        [HttpPost("processar_pagamento")]
        public async Task<IActionResult> ProcessarPagamento([FromBody] PagamentoProcessarDto dto)
        {
            string? tokenStr = GetBearerTokenOrNull();

            var retornoDTO = await _pagamentoService.ProcessarPagamento(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region POST Estornar (bloqueado)
        /// <summary>
        /// Estorno de pagamento (bloqueado).
        /// </summary>
        [HttpPost("estornar/{id}")]
        public async Task<IActionResult> EstornarPagamento(int id)
        {
            // Mantido exatamente como o original
            return StatusCode(405, "Pagamentos não podem ser Estornados.");

            // Código original comentado mantido no arquivo anterior
            // para referência. Não incluí aqui para manter foco.
        }
        #endregion

        #region PUT (bloqueado)
        /// <summary>
        /// Edição de pagamento (bloqueado).
        /// </summary>
        [HttpPut("editar/{id}")]
        public IActionResult EditarPagamento(int id)
        {
            // Mantido exatamente como o original
            return StatusCode(405, "Pagamentos não podem ser editados. Estorne ou crie um novo pagamento.");
        }
        #endregion

        #region DELETE (bloqueado)
        /// <summary>
        /// Exclusão de pagamento (bloqueado).
        /// </summary>
        [HttpDelete("excluir/{id}")]
        public IActionResult ExcluirPagamento(int id)
        {
            // Mantido exatamente como o original
            return StatusCode(405, "Pagamentos não podem ser deletados.");
        }
        #endregion

        #region PIX
        /// <summary>
        /// Gera cobrança PIX para os dados informados.
        /// </summary>
        [HttpPost("gerar-pix")]
        public async Task<IActionResult> GerarPagamentoPix([FromBody] DadosCalculoValorDto dto)
        {
            string? tokenStr = GetBearerTokenOrNull();

            var retornoDTO = await _pagamentoService.GerarPagamentoPix(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }

        /// <summary>
        /// Consulta o status de uma cobrança PIX.
        /// </summary>
        [HttpGet("status-pix")]
        public async Task<IActionResult> ConsultarStatusPix([FromQuery] string id)
        {
            var retornoDTO = await _pagamentoService.ConsultarStatusPix(id);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion
    }
}
