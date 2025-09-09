using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DELTAAPI.Service
{
    public class ProdutoService
    {
        #region Fields
        private readonly DeltaContext _context;
        private readonly ILogger<ProdutoService> _logger;
        #endregion

        #region Construtores
        public ProdutoService(ILogger<ProdutoService> logger, DeltaContext context)
        {
            _context = context;
            _logger = logger;
        }
        #endregion

        #region Métodos Públicos
        public async Task<RetornoDTO> ObterTodosProdutos(string baseUrl)
        {
            try
            {
                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ListarProdutos")
                    .AsNoTracking()
                    .ToListAsync();

                if (produtosRaw == null || produtosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum produto encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<ProdutoDto> produtos = produtosRaw
                    .Select(p => new ProdutoDto
                    {
                        ProdutoID = p.ProdutoID,
                        Nome = p.Nome,
                        Descricao = p.Descricao,
                        Preco = p.Preco,
                        Estoque = p.Estoque,
                        DataCadastro = p.DataCadastro,
                        Categorias = p.Categorias,
                        ImagemUrl = $"{baseUrl}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}",
                        Destaque = p.Destaque
                    })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Produtos obtidos com sucesso.",
                    Objeto = produtos,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produtos.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterDestaquesProdutos(string baseUrl)
        {
            try
            {
                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ListarProdutosDestaques")
                    .AsNoTracking()
                    .ToListAsync();

                if (produtosRaw == null || produtosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum produto em destaque encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<ProdutoDto> produtos = produtosRaw
                    .Select(p => new ProdutoDto
                    {
                        ProdutoID = p.ProdutoID,
                        Nome = p.Nome,
                        Descricao = p.Descricao,
                        Preco = p.Preco,
                        Estoque = p.Estoque,
                        DataCadastro = p.DataCadastro,
                        Categorias = p.Categorias,
                        ImagemUrl = $"{baseUrl}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}",
                        Destaque = p.Destaque
                    })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Produtos em destaque obtidos com sucesso.",
                    Objeto = produtos,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter produtos em destaque.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterProdutoPorIdDetalhes(int id, string baseUrl)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                await using var transaction = connection.BeginTransaction();
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;

                // 1) Incrementa a visualização
                command.CommandText = "EXEC NovaVisualizacaoProduto @ProdutoID";
                command.Parameters.Clear();
                command.Parameters.Add(new SqlParameter("@ProdutoID", id));
                await command.ExecuteNonQueryAsync();

                // 2) Busca os detalhes do produto
                command.CommandText = "EXEC ObterProdutoPorIDDetalhes @ProdutoID";
                command.Parameters.Clear();
                command.Parameters.Add(new SqlParameter("@ProdutoID", id));

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Defensive: ImagemPrincipal pode ser nula
                        string? imagemPrincipal = !reader.IsDBNull(reader.GetOrdinal("ImagemPrincipal"))
                            ? reader.GetString(reader.GetOrdinal("ImagemPrincipal"))
                            : null;

                        ProdutoDetalhesDto dto = new ProdutoDetalhesDto
                        {
                            ProdutoID = reader.GetInt32(reader.GetOrdinal("ProdutoID")),
                            Nome = reader.GetString(reader.GetOrdinal("Nome")),
                            Descricao = reader.GetString(reader.GetOrdinal("Descricao")),
                            Preco = reader.GetDecimal(reader.GetOrdinal("Preco")),
                            Estoque = reader.GetInt32(reader.GetOrdinal("Estoque")),
                            DataCadastro = reader.GetDateTime(reader.GetOrdinal("DataCadastro")),
                            ImagemUrl = !string.IsNullOrWhiteSpace(imagemPrincipal)
                                ? $"{baseUrl}/api/produto/imagem-arquivo/{Path.GetFileName(imagemPrincipal)}"
                                : null,
                            TamanhosDisponiveis = reader.IsDBNull(reader.GetOrdinal("TamanhosDisponiveis"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("TamanhosDisponiveis"))
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(t => t.Trim().ToUpper())
                                    .ToList(),
                            // BIGINT no SQL -> Int64 no C#
                            QtdVisualizacao = reader.GetInt64(reader.GetOrdinal("QtdVisualizacao"))
                        };

                        await reader.CloseAsync();
                        await transaction.CommitAsync();

                        return new RetornoDTO
                        {
                            Sucesso = true,
                            Mensagem = "Produto obtido com sucesso.",
                            Objeto = dto,
                            Status = StatusRetorno.OK
                        };
                    }
                }

                // Se não encontrou o produto, desfaz a visualização incluída (opcional).
                await transaction.RollbackAsync();

                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Produto não encontrado.",
                    Status = StatusRetorno.NotFound
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter detalhes do produto por ID.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao obter o produto.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterImagensPorProduto(int produtoId, string baseUrl)
        {
            try
            {
                var imagens = await _context.ImagemProdutos
                    .Where(i => i.ProdutoID == produtoId)
                    .Select(i => new
                    {
                        Url = $"{baseUrl}/api/produto/imagem-arquivo/{Path.GetFileName(i.Imagem)}",
                        i.ImagemPrincipal
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (imagens == null || imagens.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhuma imagem encontrada para este produto.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Imagens obtidas com sucesso.",
                    Objeto = imagens,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar imagens do produto.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro ao carregar imagens do produto.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterProdutosPorIds(List<int> ids, string baseUrl)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Lista de IDs está vazia.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                string idsConcatenados = string.Join(",", ids);
                var param = new SqlParameter("@Ids", idsConcatenados);

                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ObterProdutosPorIds @Ids", param)
                    .AsNoTracking()
                    .ToListAsync();

                if (produtosRaw == null || produtosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum produto encontrado com os IDs informados.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<ProdutoDto> produtos = produtosRaw
                    .Select(p => new ProdutoDto
                    {
                        ProdutoID = p.ProdutoID,
                        Nome = p.Nome,
                        Descricao = p.Descricao,
                        Preco = p.Preco,
                        Estoque = p.Estoque,
                        DataCadastro = p.DataCadastro,
                        Categorias = p.Categorias,
                        ImagemUrl = $"{baseUrl}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}"
                    })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Produtos obtidos com sucesso.",
                    Objeto = produtos,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos por IDs.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro ao buscar produtos por IDs.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ExcluirProduto(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarProduto @ProdutoID",
                    new SqlParameter("@ProdutoID", id)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Produto não encontrado para exclusão.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Produto deletado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (SqlException ex) when (ex.Number == 547) // Violação FK se precisar
            {
                _logger.LogError(ex, "Não é possível excluir o produto: restrição de integridade.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Não é possível excluir o produto pois ele está vinculado a outras informações.",
                    Status = StatusRetorno.BadRequest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir produto.");
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
