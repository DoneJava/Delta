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
    public class PedidoController : ControllerBase
    {
        #region Fields
        private PedidoService _PedidoService;
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
            // Lê "Authorization: Bearer {guid}"
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return Unauthorized(new { sucesso = false, mensagem = "Token não informado." });

            string header = authHeader.ToString();
            string[] parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { sucesso = false, mensagem = "Formato do token inválido." });

            if (!Guid.TryParse(parts[1], out Guid tokenGuid))
                return Unauthorized(new { sucesso = false, mensagem = "Token inválido." });

            // Reutilize seu validador central para obter o clienteId a partir do token
            // Exemplo: int? clienteId = await _PedidoService.ObterClienteIdPorToken(tokenGuid);
            int? clienteId = await _PedidoService.ObterClienteIdPorToken(tokenGuid);
            if (clienteId == null)
                return Unauthorized(new { sucesso = false, mensagem = "Token expirado ou inválido." });

            RetornoDTO retornoDTO = await _PedidoService.ObterPedidosDoCliente(clienteId.Value);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }

        #endregion

        #region GET público por número (sem login)
        [HttpGet("publico/{pedidoId:int}")]
        public async Task<IActionResult> ObterPedidoPublico(int pedidoId)
        {
            var retornoDTO = await _PedidoService.ObterPedidoPublicoPorNumero(pedidoId);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso
                    ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                    : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

    }
}
