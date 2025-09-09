#region Usings
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Pedidos")]
    public class PedidoController : ControllerBase
    {
        #region Fields
        private readonly PedidoService _PedidoService;
        #endregion

        #region Constructor
        public PedidoController(PedidoService pedidoService)
        {
            _PedidoService = pedidoService;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosPedidos()
        {
            var retornoDTO = await _PedidoService.ObterTodosPedidos();

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<IActionResult> ObterPedidoPorId(int id)
        {
            var retornoDTO = await _PedidoService.ObterPedidoPorId(id);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region POST Criar
        [HttpPost("criar")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> CriarPedido([FromBody] PedidoCreateDto dto)
        {
            var retornoDTO = await _PedidoService.CriarPedido(dto);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region PUT Atualizar
        [HttpPut("atualizar/{id}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> AtualizarPedido(int id, [FromBody] PedidoUpdateDto dto)
        {
            var retornoDTO = await _PedidoService.AtualizarPedido(id, dto);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> ExcluirPedido(int id)
        {
            var retornoDTO = await _PedidoService.ExcluirPedido(id);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region POST Registrar Contato
        [HttpPost("registrar-contato")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> RegistrarContato([FromBody] ContatoDto dto)
        {
            string? tokenStr = null;
            if (Request.Cookies.TryGetValue("token", out string? cookieToken))
                tokenStr = cookieToken;

            var retornoDTO = await _PedidoService.RegistrarContato(dto, tokenStr);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region Meus pedidos
        [HttpGet("do-cliente/{clienteId}")]
        public async Task<IActionResult> ObterPedidosDoCliente(int clienteId)
        {
            var retornoDTO = await _PedidoService.ObterPedidosDoCliente(clienteId);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }

        [HttpGet("meus")]
        public async Task<IActionResult> ObterMeusPedidos()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return Unauthorized(new { sucesso = false, mensagem = "Token não informado." });

            string header = authHeader.ToString();
            string[] parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { sucesso = false, mensagem = "Formato do token inválido." });

            if (!Guid.TryParse(parts[1], out Guid tokenGuid))
                return Unauthorized(new { sucesso = false, mensagem = "Token inválido." });

            int? clienteId = await _PedidoService.ObterClienteIdPorToken(tokenGuid);
            if (clienteId == null)
                return Unauthorized(new { sucesso = false, mensagem = "Token expirado ou inválido." });

            var retornoDTO = await _PedidoService.ObterPedidosDoCliente(clienteId.Value);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region GET público por número (sem login) - NOVO FLUXO

        public sealed class PedidoPublicoRequest
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int PedidoId { get; set; }

            [Required]
            [StringLength(20)]
            public string CPF { get; set; } = string.Empty; // com/sem máscara
        }

        [HttpPost("publico/buscar")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PedidoCompletoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BuscarPedidoPublico([FromBody] PedidoPublicoRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos.", erros = ModelState });

            // certifique-se que o nome do field DI está correto (_pedidoService)
            var ret = await _PedidoService.ObterPedidoPublicoPorNumeroECpf(req.PedidoId, req.CPF);

            return StatusCode((int)ret.Status,
                ret.Sucesso
                    ? ret.Objeto ?? new { mensagem = ret.Mensagem }
                    : new { sucesso = false, mensagem = ret.Mensagem });
        }

        // ponte de compatibilidade (mantém rota antiga fora do Swagger)
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("publico/{pedidoId:int}")]
        public IActionResult Obsoleto(int pedidoId, [FromQuery] string? cpf = null)
        {
            return BadRequest(new
            {
                sucesso = false,
                mensagem = "Endpoint alterado. Use POST /api/pedido/publico/buscar com JSON { pedidoId, cpf }."
            });
        }

        #endregion

    }
}
