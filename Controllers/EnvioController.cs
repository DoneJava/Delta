#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using DELTAAPI.Service;
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
        private EnvioService _EnvioService;
        #endregion

        #region Constructor
        public EnvioController(EnvioService clienteService)
        {
            _EnvioService = clienteService;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosEnvios()
        {
            RetornoDTO retornoDTO = await _EnvioService.ObterTodosEnvios();

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
        public async Task<IActionResult> ObterEnvioPorId(int id)
        {
            RetornoDTO retornoDTO = await _EnvioService.ObterEnvioPorId(id);

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

        #region POST Criar
        [HttpPost("criar")]
        public async Task<IActionResult> CriarEnvio([FromBody] EnvioCreateDto dto)
        {
            RetornoDTO retornoDTO = await _EnvioService.CriarEnvio(dto);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion

        #region PUT Atualizar
        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarEnvio(int id, [FromBody] EnvioUpdateDto dto)
        {
            RetornoDTO retornoDTO = await _EnvioService.AtualizarEnvio(id, dto);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarEnvio(int id)
        {
            RetornoDTO retornoDTO = await _EnvioService.DeletarEnvio(id);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion
    }
}