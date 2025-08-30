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
    public class ItemPedidoController : ControllerBase
    {
        #region Fields
        private ItemPedidoService _ItemPedidoService;
        #endregion

        #region Constructor
        public ItemPedidoController(ItemPedidoService itemPedidoService)
        {
            _ItemPedidoService = itemPedidoService;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosItensPedido()
        {
            RetornoDTO retornoDTO = await _ItemPedidoService.ObterTodosItensPedido();

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
        public async Task<IActionResult> ObterItemPedidoPorId(int id)
        {
            RetornoDTO retornoDTO = await _ItemPedidoService.ObterItemPedidoPorId(id);

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

        #region POST Inserir
        [HttpPost("inserir")]
        public async Task<IActionResult> InserirItemPedido([FromBody] ItemPedidoCreateDto dto)
        {
            RetornoDTO retornoDTO = await _ItemPedidoService.InserirItemPedido(dto);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion

        #region PUT Atualizar
        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarItemPedido(int id, [FromBody] ItemPedidoUpdateDto dto)
        {
            RetornoDTO retornoDTO = await _ItemPedidoService.AtualizarItemPedido(id, dto);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarItemPedido(int id)
        {
            RetornoDTO retornoDTO = await _ItemPedidoService.DeletarItemPedido(id);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion
    }
}
