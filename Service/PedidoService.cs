using DELTAAPI.Controllers;
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
                            NomeCliente = cliente.Nome,
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
                if (match.Success && Int32.TryParse(match.Groups[1].Value, out int idPedidoEncontrado))
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
                var paramCliente = new SqlParameter("@ClienteID", clienteId);

                var pedidos = await _context.Set<PedidoDto>()
                    .FromSqlRaw("EXEC ListarPedidosPorClienteID @ClienteID", paramCliente)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var pedido in pedidos)
                {
                    var paramPedido = new SqlParameter("@PedidoID", pedido.PedidoID);

                    var itens = await _context.Set<ItemDto>()
                        .FromSqlRaw("EXEC ListarItensPorPedidoID @PedidoID", paramPedido)
                        .AsNoTracking()
                        .ToListAsync();

                    pedido.Itens = itens;
                }

                if (pedidos == null || pedidos.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum pedido encontrado para este cliente.",
                        Status = StatusRetorno.NotFound
                    };
                }

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
                _logger.LogError(ex, "Erro ao obter pedidos do cliente.");
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
