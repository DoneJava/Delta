#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Models;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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
        #endregion
    }
}
