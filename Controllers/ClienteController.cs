#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Helpers;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public ClienteController(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region GET Validar Token
        [HttpGet("validar-token")]
        public async Task<IActionResult> ValidarToken()
        {
            try
            {
                string? tokenStr = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (string.IsNullOrEmpty(tokenStr))
                    return BadRequest("Token não fornecido.");

                if (!Guid.TryParse(tokenStr, out Guid token))
                    return BadRequest("Token inválido.");

                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Token == token);

                if (cliente == null)
                    return Unauthorized("Token inválido.");

                if (cliente.ValidadeToken < DateTime.UtcNow)
                    return Unauthorized("Token expirado.");

                return Ok(new
                {
                    mensagem = "Token válido.",
                    clienteId = cliente.ClienteID,
                    nome = cliente.Nome,
                    email = cliente.Email
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
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

                if (string.IsNullOrEmpty(tokenStr))
                    return BadRequest("Token não fornecido.");

                if (!Guid.TryParse(tokenStr, out Guid token))
                    return BadRequest("Token inválido.");

                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Token == token);

                if (cliente == null)
                    return NotFound("Cliente não encontrado.");

                return Ok(new
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
                return StatusCode(500, "Erro ao obter dados do cliente.");
            }
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<Cliente>>> ObterTodosClientes()
        {
            try
            {
                List<Cliente> clientes = await _context.Clientes
                    .FromSqlRaw("EXEC ListarClientes")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(clientes);
            }
            catch (Exception)
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST
        [HttpPost("inserir")]
        public async Task<IActionResult> InserirCliente([FromBody] ClienteCreateDto dto)
        {
            try
            {
                byte[] senhaHash = SecurityHelper.GeneratePasswordHash(dto.SenhaEmTexto, out byte[] salt);
                byte[] senhaFinal = senhaHash.Concat(salt).ToArray();

                Guid token = Guid.NewGuid();
                DateTime validadeToken = DateTime.UtcNow.AddMonths(1);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirCliente @Nome, @CPF_CNPJ, @Email, @Senha, @Telefone, @Endereco, @Complemento, @CEP, @Portaria24Horas, @Token, @ValidadeToken",
                    new SqlParameter("@Nome", dto.Nome),
                    new SqlParameter("@CPF_CNPJ", dto.CPF_CNPJ),
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Senha", senhaFinal),
                    new SqlParameter("@Telefone", (object?)dto.Telefone ?? DBNull.Value),
                    new SqlParameter("@Endereco", (object?)dto.Endereco ?? DBNull.Value),
                    new SqlParameter("@Complemento", (object?)dto.Complemento ?? DBNull.Value),
                    new SqlParameter("@CEP", (object?)dto.CEP ?? DBNull.Value),
                    new SqlParameter("@Portaria24Horas", (object?)dto.Portaria24Horas ?? DBNull.Value),
                    new SqlParameter("@Token", token),
                    new SqlParameter("@ValidadeToken", validadeToken)
                );

                return Ok(new { mensagem = "Cliente inserido com sucesso.", token = token, validade = validadeToken });
            }
            catch (SqlException ex) when (ex.Message.Contains("UQ__Cliente__") || ex.Message.Contains("chave duplicada"))
            {
                string mensagemErro;

                if (ex.Message.Contains(dto.CPF_CNPJ))
                    mensagemErro = "Já existe um cliente com este CPF.";
                else if (ex.Message.Contains(dto.Email))
                    mensagemErro = "Já existe um cliente com este e-mail.";
                else
                    mensagemErro = "Já existe um cliente com os dados informados.";

                return Conflict(mensagemErro);
            }
            catch (Exception)
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region PUT
        [HttpPut("atualizar")]
        public async Task<IActionResult> AtualizarCliente([FromBody] ClienteUpdateDto dto)
        {
            try
            {
                string? tokenStr = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (!Guid.TryParse(tokenStr, out Guid token))
                    return Unauthorized("Token inválido.");

                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Token == token);

                if (cliente == null)
                    return NotFound("Cliente não encontrado.");

                // Verificar duplicidade de email e CPF, desconsiderando o próprio cliente
                bool emailExiste = await _context.Clientes
                    .AnyAsync(c => c.Email == dto.Email && c.ClienteID != cliente.ClienteID);

                if (emailExiste)
                    return Conflict("Já existe um cliente com este e-mail.");

                bool cpfExiste = await _context.Clientes
                    .AnyAsync(c => c.CPF_CNPJ == dto.CPF_CNPJ && c.ClienteID != cliente.ClienteID);

                if (cpfExiste)
                    return Conflict("Já existe um cliente com este CPF.");

                byte[] novaSenhaHash;
                bool senhaAtualVazia = string.IsNullOrWhiteSpace(dto.SenhaAtual);
                bool novaSenhaVazia = string.IsNullOrWhiteSpace(dto.SenhaEmTexto);

                if (!senhaAtualVazia && !novaSenhaVazia)
                {
                    byte[] stored = cliente.Senha!;
                    byte[] hash = stored.Take(32).ToArray();
                    byte[] salt = stored.Skip(32).ToArray();

                    if (!SecurityHelper.VerifyPassword(dto.SenhaAtual, hash, salt))
                        return Unauthorized("Senha atual incorreta.");

                    byte[] novaHash = SecurityHelper.GeneratePasswordHash(dto.SenhaEmTexto, out byte[] novoSalt);
                    novaSenhaHash = novaHash.Concat(novoSalt).ToArray();
                }
                else if (senhaAtualVazia && novaSenhaVazia)
                {
                    novaSenhaHash = cliente.Senha!;
                }
                else
                {
                    return BadRequest("Para alterar a senha, preencha a senha atual e a nova senha.");
                }

                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AtualizarCliente @ClienteID, @Nome, @CPF_CNPJ, @Email, @Senha, @Telefone, @Endereco, @Complemento, @CEP, @Portaria24Horas",
                    new SqlParameter("@ClienteID", cliente.ClienteID),
                    new SqlParameter("@Nome", dto.Nome),
                    new SqlParameter("@CPF_CNPJ", dto.CPF_CNPJ),
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Senha", novaSenhaHash),
                    new SqlParameter("@Telefone", (object?)dto.Telefone ?? DBNull.Value),
                    new SqlParameter("@Endereco", (object?)dto.Endereco ?? DBNull.Value),
                    new SqlParameter("@Complemento", (object?)dto.Complemento ?? DBNull.Value),
                    new SqlParameter("@CEP", (object?)dto.CEP ?? DBNull.Value),
                    new SqlParameter("@Portaria24Horas", (object?)dto.Portaria24Horas ?? DBNull.Value)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Cliente não encontrado para atualização.");

                return Ok("Cliente atualizado com sucesso.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarCliente(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarCliente @ClienteID",
                    new SqlParameter("@ClienteID", id)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Cliente não encontrado para exclusão.");

                return Ok("Cliente deletado com sucesso.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == dto.Email);

                if (cliente == null || cliente.Senha == null)
                    return Unauthorized("E-mail ou senha incorretos.");

                byte[] stored = cliente.Senha;
                byte[] hash = stored.Take(32).ToArray();
                byte[] salt = stored.Skip(32).ToArray();

                bool senhaCorreta = SecurityHelper.VerifyPassword(dto.Senha, hash, salt);

                if (!senhaCorreta)
                    return Unauthorized("E-mail ou senha incorretos.");

                Guid token = Guid.NewGuid();
                DateTime validade = DateTime.UtcNow.AddHours(1);

                cliente.Token = token;
                cliente.ValidadeToken = validade;

                _context.Clientes.Update(cliente);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    token = token.ToString(),
                    validade = validade
                });
            }
            catch
            {
                return StatusCode(500, "Erro ao tentar realizar login. Por favor, tente novamente mais tarde.");
            }
        }
        #endregion
    }
}
