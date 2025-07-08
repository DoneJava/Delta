using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Helpers;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    public class ClienteService
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Construtores
        public ClienteService(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region Métodos Públicos
        public async Task<RetornoDTO> ValidarToken(string tokenStr)
        {     
            if (string.IsNullOrEmpty(tokenStr))
                return new RetornoDTO { Sucesso = false, Mensagem = "Token não fornecido.", Status = StatusRetorno.BadRequest };

            if (!Guid.TryParse(tokenStr, out Guid token))
                return new RetornoDTO { Sucesso = false, Mensagem = "Token inválido.", Status = StatusRetorno.BadRequest };

            Cliente? cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Token == token);

            if (cliente == null)
                return new RetornoDTO { Sucesso = false, Mensagem = "Token inválido.", Status = StatusRetorno.Unauthorized };

            if (cliente.ValidadeToken < DateTime.UtcNow)
                return new RetornoDTO { Sucesso = false, Mensagem = "Token expirado.", Status = StatusRetorno.Unauthorized };

            return new RetornoDTO { Sucesso = true, Mensagem = "Token válido.", Objeto = cliente, Status = StatusRetorno.OK };
        }

        public async Task<RetornoDTO> ObterDadosClienteAutenticado(string tokenStr)
        {
            if (string.IsNullOrEmpty(tokenStr))
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Token não fornecido.",
                    Status = StatusRetorno.BadRequest
                };
            }

            if (!Guid.TryParse(tokenStr, out Guid token))
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Token inválido.",
                    Status = StatusRetorno.BadRequest
                };
            }

            Cliente? cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Token == token);

            if (cliente == null)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Cliente não encontrado.",
                    Status = StatusRetorno.NotFound
                };
            }

            return new RetornoDTO
            {
                Sucesso = true,
                Mensagem = "Dados do cliente obtidos com sucesso.",
                Objeto = cliente,
                Status = StatusRetorno.OK
            };
        }

        public async Task<RetornoDTO> ObterTodosClientes()
        {
            try
            {
                List<Cliente> clientes = await _context.Clientes
                    .FromSqlRaw("EXEC ListarClientes")
                    .AsNoTracking()
                    .ToListAsync();

                if (clientes == null || clientes.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum cliente encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Clientes obtidos com sucesso.",
                    Objeto = clientes,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro ao obter clientes.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> InserirCliente(ClienteCreateDto dto)
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

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Cliente inserido com sucesso.",
                    Status = StatusRetorno.Created,
                    Objeto = new
                    {
                        Token = token,
                        ValidadeToken = validadeToken
                    }
                };
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

                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = mensagemErro,
                    Status = StatusRetorno.Conflict
                };
            }
            catch (Exception)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> AtualizarCliente(string tokenStr, ClienteUpdateDto dto)
        {
            if (!Guid.TryParse(tokenStr, out Guid token))
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Token inválido.",
                    Status = StatusRetorno.Unauthorized
                };
            }

            Cliente? cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Token == token);

            if (cliente == null)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Cliente não encontrado.",
                    Status = StatusRetorno.NotFound
                };
            }

            // Verificar duplicidade de email
            bool emailExiste = await _context.Clientes
                .AnyAsync(c => c.Email == dto.Email && c.ClienteID != cliente.ClienteID);

            if (emailExiste)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Já existe um cliente com este e-mail.",
                    Status = StatusRetorno.Conflict
                };
            }

            // Verificar duplicidade de CPF/CNPJ
            bool cpfExiste = await _context.Clientes
                .AnyAsync(c => c.CPF_CNPJ == dto.CPF_CNPJ && c.ClienteID != cliente.ClienteID);

            if (cpfExiste)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Já existe um cliente com este CPF.",
                    Status = StatusRetorno.Conflict
                };
            }

            // Gerenciar senha
            byte[] novaSenhaHash;
            bool senhaAtualVazia = string.IsNullOrWhiteSpace(dto.SenhaAtual);
            bool novaSenhaVazia = string.IsNullOrWhiteSpace(dto.SenhaEmTexto);

            if (!senhaAtualVazia && !novaSenhaVazia)
            {
                byte[] stored = cliente.Senha!;
                byte[] hash = stored.Take(32).ToArray();
                byte[] salt = stored.Skip(32).ToArray();

                if (!SecurityHelper.VerifyPassword(dto.SenhaAtual, hash, salt))
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Senha atual incorreta.",
                        Status = StatusRetorno.Unauthorized
                    };
                }

                byte[] novaHash = SecurityHelper.GeneratePasswordHash(dto.SenhaEmTexto, out byte[] novoSalt);
                novaSenhaHash = novaHash.Concat(novoSalt).ToArray();
            }
            else if (senhaAtualVazia && novaSenhaVazia)
            {
                novaSenhaHash = cliente.Senha!;
            }
            else
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Para alterar a senha, preencha a senha atual e a nova senha.",
                    Status = StatusRetorno.BadRequest
                };
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
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Cliente não encontrado para atualização.",
                    Status = StatusRetorno.NotFound
                };
            }

            return new RetornoDTO
            {
                Sucesso = true,
                Mensagem = "Cliente atualizado com sucesso.",
                Status = StatusRetorno.OK
            };
        }

        public async Task<RetornoDTO> DeletarCliente(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarCliente @ClienteID",
                    new SqlParameter("@ClienteID", id)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Cliente não encontrado para exclusão.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Cliente deletado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> Login(LoginDto dto)
        {
            try
            {
                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == dto.Email);

                if (cliente == null || cliente.Senha == null)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "E-mail ou senha incorretos.",
                        Status = StatusRetorno.Unauthorized
                    };
                }

                byte[] stored = cliente.Senha;
                byte[] hash = stored.Take(32).ToArray();
                byte[] salt = stored.Skip(32).ToArray();

                bool senhaCorreta = SecurityHelper.VerifyPassword(dto.Senha, hash, salt);

                if (!senhaCorreta)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "E-mail ou senha incorretos.",
                        Status = StatusRetorno.Unauthorized
                    };
                }

                Guid token = Guid.NewGuid();
                DateTime validade = DateTime.UtcNow.AddHours(1);

                cliente.Token = token;
                cliente.ValidadeToken = validade;

                _context.Clientes.Update(cliente);
                await _context.SaveChangesAsync();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Login realizado com sucesso.",
                    Status = StatusRetorno.OK,
                    Objeto = new
                    {
                        Token = token,
                        Validade = validade
                    }
                };
            }
            catch
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro ao tentar realizar login. Por favor, tente novamente mais tarde.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        #endregion
    }
}
