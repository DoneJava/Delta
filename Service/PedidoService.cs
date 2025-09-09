using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Data;

namespace DELTAAPI.Service
{
    public class PedidoService
    {
        #region Fields
        private readonly DeltaContext _context;
        private readonly ILogger<PedidoService> _logger;
        #endregion

        #region Construtores
        public PedidoService(ILogger<PedidoService> logger, DeltaContext context)
        {
            _context = context;
            _logger = logger;
        }
        #endregion

        #region Helpers
        private static string NormalizeCpfDigits(string? cpf)
            => new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        #endregion

        #region Métodos Públicos
        public async Task<RetornoDTO> ObterTodosPedidos()
        {
            try
            {
                List<Pedido> pedidosRaw = await _context.Pedidos
                    .FromSqlRaw("EXEC ListarPedidos")
                    .AsNoTracking()
                    .ToListAsync();

                if (pedidosRaw == null || pedidosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum pedido encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<Cliente> clientes = await _context.Clientes
                    .AsNoTracking()
                    .ToListAsync();

                List<PedidoDto> pedidos = pedidosRaw
                    .Join(clientes,
                        pedido => pedido.ClienteID,
                        cliente => cliente.ClienteID,
                        (pedido, cliente) => new PedidoDto
                        {
                            PedidoID = pedido.PedidoID,
                            ClienteID = pedido.ClienteID,
                            NomeCliente = cliente.Nome ?? "Nome não encontrado",
                            DataPedido = pedido.DataPedido,
                            Status = pedido.Status,
                            ValorTotal = pedido.ValorTotal
                        })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pedidos obtidos com sucesso.",
                    Objeto = pedidos,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pedidos.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterPedidoPorId(int id)
        {
            try
            {
                SqlParameter parametro = new SqlParameter("@PedidoID", id);

                List<Pedido> pedidos = await _context.Pedidos
                    .FromSqlRaw("EXEC ObterPedidoPorID @PedidoID", parametro)
                    .AsNoTracking()
                    .ToListAsync();

                Pedido? pedido = pedidos.FirstOrDefault();

                if (pedido == null)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Pedido não encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                Cliente? cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClienteID == pedido.ClienteID);

                PedidoDto dto = new PedidoDto
                {
                    PedidoID = pedido.PedidoID,
                    ClienteID = pedido.ClienteID,
                    NomeCliente = cliente?.Nome ?? string.Empty,
                    DataPedido = pedido.DataPedido,
                    Status = pedido.Status,
                    ValorTotal = pedido.ValorTotal
                };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pedido obtido com sucesso.",
                    Objeto = dto,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pedido por ID.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> CriarPedido(PedidoCreateDto dto)
        {
            try
            {
                bool clienteExiste = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.ClienteID == dto.ClienteID);

                if (!clienteExiste)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Cliente informado não existe.",
                        Status = StatusRetorno.NotFound
                    };
                }

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirPedido @ClienteID, @Status, @ValorTotal",
                    new SqlParameter("@ClienteID", dto.ClienteID),
                    new SqlParameter("@Status", dto.Status),
                    new SqlParameter("@ValorTotal", dto.ValorTotal)
                );

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pedido criado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar pedido.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> AtualizarPedido(int id, PedidoUpdateDto dto)
        {
            if (id != dto.PedidoID)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "ID inconsistente.",
                    Status = StatusRetorno.BadRequest
                };
            }

            try
            {
                bool clienteExiste = await _context.Clientes
                    .AsNoTracking()
                    .AnyAsync(c => c.ClienteID == dto.ClienteID);

                if (!clienteExiste)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Cliente informado não existe.",
                        Status = StatusRetorno.NotFound
                    };
                }

                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AtualizarPedido @PedidoID, @ClienteID, @Status, @ValorTotal",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ClienteID", dto.ClienteID),
                    new SqlParameter("@Status", dto.Status),
                    new SqlParameter("@ValorTotal", dto.ValorTotal)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Pedido não encontrado para atualização.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pedido atualizado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (SqlException ex) when (ex.Number == 50000 || ex.Number == 547)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro de integridade: " + ex.Message,
                    Status = StatusRetorno.BadRequest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar pedido.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ExcluirPedido(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarPedido @PedidoID",
                    new SqlParameter("@PedidoID", id)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Pedido não encontrado para exclusão.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pedido deletado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Não é possível excluir o pedido: ele está relacionado a outros dados.",
                    Status = StatusRetorno.BadRequest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir pedido.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> RegistrarContato(ContatoDto dto, string? tokenStr)
        {
            try
            {
                int? clienteId = null;
                int? pedidoId = null;

                // Verificar token do cookie (opcional)
                if (!string.IsNullOrWhiteSpace(tokenStr))
                {
                    if (Guid.TryParse(tokenStr, out Guid token))
                    {
                        Cliente? cliente = await _context.Clientes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Token == token && c.ValidadeToken > DateTime.UtcNow);

                        if (cliente != null)
                            clienteId = cliente.ClienteID;
                    }
                }

                // Extrair ID de pedido no texto [ID]
                Match match = Regex.Match(dto.Assunto + " " + dto.Mensagem, "\\[(\\d+)\\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idPedidoEncontrado))
                {
                    bool pedidoExiste = await _context.Pedidos
                        .AsNoTracking()
                        .AnyAsync(p => p.PedidoID == idPedidoEncontrado);

                    if (pedidoExiste)
                        pedidoId = idPedidoEncontrado;
                }

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC RegistrarContato @Nome, @Email, @Assunto, @Mensagem, @ClienteId, @PedidoId",
                    new SqlParameter("@Nome", dto.Nome),
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Assunto", dto.Assunto),
                    new SqlParameter("@Mensagem", dto.Mensagem),
                    new SqlParameter("@ClienteId", clienteId ?? (object)DBNull.Value),
                    new SqlParameter("@PedidoId", pedidoId ?? (object)DBNull.Value)
                );

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Contato registrado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar contato.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterPedidosDoCliente(int clienteId)
        {
            try
            {
                var pCliente = new SqlParameter("@ClienteID", clienteId);
                var rows = await _context.Set<PedidoCompletoDto>()
                    .FromSqlRaw("EXEC dbo.ListarPedidosCompletoPorClienteID @ClienteID", pCliente)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var r in rows)
                {
                    r.Itens = string.IsNullOrWhiteSpace(r.ItensJson)
                        ? new List<ItemDto>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<ItemDto>>(r.ItensJson) ?? new List<ItemDto>();

                    foreach (var it in r.Itens)
                    {
                        if (string.IsNullOrEmpty(it.TamanhoSelecionado))
                            it.TamanhoSelecionado = it.Tamanho ?? "";
                    }

                    // Fallback de desconto (se a proc não popular por algum motivo)
                    {
                        // 1) Bruto: usa o que veio da proc; se vier 0, soma pelos itens
                        decimal bruto = r.ValorItensBruto > 0m
                            ? r.ValorItensBruto
                            : (r.Itens?.Sum(i =>
                                    // PrecoUnitario é decimal não anulável
                                    (i.PrecoUnitario > 0m ? i.PrecoUnitario : 0m) *
                                    // Quantidade é int não anulável
                                    (i.Quantidade > 0 ? i.Quantidade : 1)
                               ) ?? 0m);

                        // 2) Líquido: total - frete (travado em zero)
                        decimal liquido = r.ValorTotal - r.ValorFrete;
                        if (liquido < 0m) liquido = 0m;

                        // 3) Desconto calculado (travado em zero)
                        decimal descontoCalc = bruto - liquido;
                        if (descontoCalc < 0m) descontoCalc = 0m;

                        // 4) Usa o calculado se o campo veio zerado/negativo
                        // Se 'DescontoAplicado' for decimal não anulável:
                        if (r.DescontoAplicado <= 0m)
                            r.DescontoAplicado = descontoCalc;
                    }
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = rows.Count == 0 ? "Nenhum pedido encontrado para este cliente." : "Pedidos obtidos com sucesso.",
                    Objeto = rows,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pedidos do cliente.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<int?> ObterClienteIdPorToken(Guid token)
        {
            try
            {
                SqlParameter pToken = new SqlParameter("@Token", token);
                List<ClienteIdDto> dados = await _context.Set<ClienteIdDto>()
                    .FromSqlRaw("EXEC SEC_sp_ObterClienteIdPorToken @Token", pToken)
                    .AsNoTracking()
                    .ToListAsync();

                ClienteIdDto? row = dados.FirstOrDefault();
                return row?.ClienteId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter Cliente id por token.");
                throw;
            }
        }

        // Novo fluxo: número + CPF (somente 11 dígitos)
        public async Task<RetornoDTO> ObterPedidoPublicoPorNumeroECpf(int pedidoId, string cpf)
        {
            try
            {
                var cpfDigits = NormalizeCpfDigits(cpf);
                if (cpfDigits.Length != 11)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "CPF inválido. Informe 11 dígitos (com ou sem máscara).",
                        Status = StatusRetorno.BadRequest
                    };
                }

                var pPedido = new SqlParameter("@PedidoID", pedidoId);
                var pCpf = new SqlParameter("@CPF", SqlDbType.VarChar, 20) { Value = cpfDigits };

                var rows = await _context.Set<PedidoCompletoDto>()
                    .FromSqlRaw("EXEC dbo.ListarPedidoCompletoPorPedidoID_E_CPF @PedidoID, @CPF", pPedido, pCpf)
                    .AsNoTracking()
                    .ToListAsync();

                var r = rows.FirstOrDefault();

                if (r != null)
                {
                    // ItensJson -> Itens (com PrecoUnitario e Tamanho/TamanhoSelecionado)
                    r.Itens = string.IsNullOrWhiteSpace(r.ItensJson)
                        ? new List<ItemDto>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<ItemDto>>(r.ItensJson) ?? new List<ItemDto>();

                    // segurança: garante propriedades esperadas
                    foreach (var it in r.Itens)
                    {
                        // usa somente o unitário salvo no pedido (sem cupom)
                        if (it.PrecoUnitario == 0 && it.PrecoUnitario > 0)
                            it.PrecoUnitario = it.PrecoUnitario;

                        // alias do tamanho
                        if (string.IsNullOrEmpty(it.TamanhoSelecionado))
                            it.TamanhoSelecionado = it.Tamanho ?? it.Tamanho ?? "";
                    }
                }

                return new RetornoDTO
                {
                    Sucesso = r != null,
                    Mensagem = r != null ? "Pedido obtido com sucesso." : "Pedido não encontrado.",
                    Objeto = r,
                    Status = r != null ? StatusRetorno.OK : StatusRetorno.NotFound
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pedido público por número e CPF.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        #endregion
    }
}
