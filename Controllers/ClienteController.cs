#region Usings
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        #region Fields
        private ClienteService _ClienteService;
        #endregion

        #region Constructor
        public ClienteController(ClienteService clienteService)
        {
            _ClienteService = clienteService;
        }
        #endregion

        #region GET Validar Token
        [HttpGet("validar-token")]
        public async Task<IActionResult> ValidarToken()
        {
            try
            {
                string? tokenStr = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                RetornoDTO retornoDTO = await _ClienteService.ValidarToken(tokenStr);

                if (!retornoDTO.Sucesso)
                {
                    return StatusCode((int)retornoDTO.Status, new
                    {
                        mensagem = retornoDTO.Mensagem
                    });
                }

                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem,
                    clienteId = retornoDTO.Objeto.ClienteID,
                    nome = retornoDTO.Objeto.Nome,
                    email = retornoDTO.Objeto.Email
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!"
                });
            }
        }
        #endregion

        #region GET Obter Dados do Cliente Autenticado
        [HttpGet("obter-dados")]
        public async Task<IActionResult> ObterDadosClienteAutenticado()
        {
            try
            {
                string? tokenStr = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                RetornoDTO retornoDTO = await _ClienteService.ObterDadosClienteAutenticado(tokenStr);

                if (!retornoDTO.Sucesso)
                {
                    return StatusCode((int)retornoDTO.Status, new
                    {
                        mensagem = retornoDTO.Mensagem
                    });
                }

                Cliente cliente = (Cliente)retornoDTO.Objeto;

                return StatusCode((int)retornoDTO.Status, new
                {
                    cliente.ClienteID,
                    cliente.Nome,
                    cliente.Email,
                    cliente.CPF_CNPJ,
                    cliente.Telefone,
                    cliente.Endereco,
                    cliente.Complemento,
                    cliente.CEP,
                    cliente.Portaria24Horas
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensagem = "Erro ao obter dados do cliente."
                });
            }
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosClientes()
        {
            try
            {
                RetornoDTO retornoDTO = await _ClienteService.ObterTodosClientes();

                if (!retornoDTO.Sucesso)
                {
                    return StatusCode((int)retornoDTO.Status, new
                    {
                        mensagem = retornoDTO.Mensagem
                    });
                }

                return StatusCode((int)retornoDTO.Status, retornoDTO.Objeto);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!"
                });
            }
        }
        #endregion

        #region POST Inserir
        [HttpPost("inserir")]
        public async Task<IActionResult> InserirCliente([FromBody] ClienteCreateDto dto)
        {
            RetornoDTO retornoDTO = await _ClienteService.InserirCliente(dto);

            if (!retornoDTO.Sucesso)
            {
                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem,
                token = retornoDTO.Objeto.Token,
                validade = retornoDTO.Objeto.ValidadeToken
            });
        }
        #endregion

        #region PUT Atualizar
        [HttpPut("atualizar")]
        public async Task<IActionResult> AtualizarCliente([FromBody] ClienteUpdateDto dto)
        {
            try
            {
                string? tokenStr = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                RetornoDTO retornoDTO = await _ClienteService.AtualizarCliente(tokenStr, dto);

                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!"
                });
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarCliente(int id)
        {
            RetornoDTO retornoDTO = await _ClienteService.DeletarCliente(id);

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem
            });
        }
        #endregion

        #region POST Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            RetornoDTO retornoDTO = await _ClienteService.Login(dto);

            if (!retornoDTO.Sucesso)
            {
                return StatusCode((int)retornoDTO.Status, new
                {
                    mensagem = retornoDTO.Mensagem
                });
            }

            return StatusCode((int)retornoDTO.Status, new
            {
                mensagem = retornoDTO.Mensagem,
                token = retornoDTO.Objeto.Token,
                validade = retornoDTO.Objeto.Validade
            });
        }
        #endregion
    }
}
