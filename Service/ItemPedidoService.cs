using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    public class ItemPedidoService
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Construtores
        public ItemPedidoService(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region Métodos Públicos
        public async Task<RetornoDTO> ObterTodosItensPedido()
        {
            try
            {
                List<ItemPedido> itensPedido = await _context.ItensPedido
                    .FromSqlRaw("EXEC ListarItensPedido")
                    .AsNoTracking()
                    .ToListAsync();

                if (itensPedido == null || itensPedido.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum item de pedido encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<Produto> produtos = await _context.Produtos
                    .AsNoTracking()
                    .ToListAsync();

                List<ItemPedidoDto> itensDto = itensPedido
                    .Join(produtos,
                        i => i.ProdutoID,
                        p => p.ProdutoID,
                        (i, p) => new ItemPedidoDto
                        {
                            ItemPedidoID = i.ItemPedidoID,
                            PedidoID = i.PedidoID,
                            ProdutoID = i.ProdutoID,
                            NomeProduto = p.Nome,
                            Quantidade = i.Quantidade,
                            PrecoUnitario = i.PrecoUnitario
                        })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Itens de pedido obtidos com sucesso.",
                    Objeto = itensDto,
                    Status = StatusRetorno.OK
                };
            }
            catch
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterItemPedidoPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@ItemPedidoID", id);

                ItemPedido? item = await Task.Run(() =>
                    _context.ItensPedido
                        .FromSqlRaw("EXEC ObterItemPedidoPorID @ItemPedidoID", param)
                        .AsNoTracking()
                        .AsEnumerable()
                        .FirstOrDefault());

                if (item == null)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Item de pedido não encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                Produto? produto = await _context.Produtos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProdutoID == item.ProdutoID);

                ItemPedidoDto dto = new ItemPedidoDto
                {
                    ItemPedidoID = item.ItemPedidoID,
                    PedidoID = item.PedidoID,
                    ProdutoID = item.ProdutoID,
                    NomeProduto = produto?.Nome ?? "",
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario
                };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Item de pedido obtido com sucesso.",
                    Objeto = dto,
                    Status = StatusRetorno.OK
                };
            }
            catch
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> InserirItemPedido(ItemPedidoCreateDto dto)
        {
            try
            {
                bool pedidoExiste = await _context.Pedidos
                    .AsNoTracking()
                    .AnyAsync(p => p.PedidoID == dto.PedidoID);

                if (!pedidoExiste)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = $"Pedido com ID {dto.PedidoID} não encontrado.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                bool produtoExiste = await _context.Produtos
                    .AsNoTracking()
                    .AnyAsync(p => p.ProdutoID == dto.ProdutoID);

                if (!produtoExiste)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = $"Produto com ID {dto.ProdutoID} não encontrado.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirItemPedido @PedidoID, @ProdutoID, @Quantidade, @PrecoUnitario",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ProdutoID", dto.ProdutoID),
                    new SqlParameter("@Quantidade", dto.Quantidade),
                    new SqlParameter("@PrecoUnitario", dto.PrecoUnitario)
                );

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Item de pedido inserido com sucesso.",
                    Status = StatusRetorno.Created
                };
            }
            catch (SqlException ex)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = $"Erro de banco de dados: {ex.Message}",
                    Status = StatusRetorno.InternalServerError
                };
            }
            catch (DbUpdateException ex)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = $"Erro de atualização no banco: {ex.Message}",
                    Status = StatusRetorno.InternalServerError
                };
            }
            catch
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> AtualizarItemPedido(int id, ItemPedidoUpdateDto dto)
        {
            if (id != dto.ItemPedidoID)
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
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC AtualizarItemPedido @ItemPedidoID, @PedidoID, @ProdutoID, @Quantidade, @PrecoUnitario",
                    new SqlParameter("@ItemPedidoID", dto.ItemPedidoID),
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ProdutoID", dto.ProdutoID),
                    new SqlParameter("@Quantidade", dto.Quantidade),
                    new SqlParameter("@PrecoUnitario", dto.PrecoUnitario)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Item de pedido não encontrado para atualização.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Item de pedido atualizado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> DeletarItemPedido(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarItemPedido @ItemPedidoID",
                    new SqlParameter("@ItemPedidoID", id)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Item de pedido não encontrado para exclusão.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Item de pedido deletado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch
            {
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
