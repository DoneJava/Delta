#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public ProdutoController(DeltaContext context)
        {
            _context = context; 
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> ObterTodosProdutos()
        {
            try
            {
                // Executa o procedimento armazenado para obter todos os produtos
                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ListarProdutos")
                    .AsNoTracking()
                    .ToListAsync();

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
                        ImagemUrl = $"{Request.Scheme}://{Request.Host}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}",
                        Destaque = p.Destaque,
                    })
                    .ToList();

                return Ok(produtos);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET Destaques
        [HttpGet("obter-destaques")]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> ObterDestaquesProdutos()
        {
            try
            {
                // Executa o procedimento armazenado para obter os produtos destaque
                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ListarProdutosDestaques")
                    .AsNoTracking()
                    .ToListAsync();

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
                        ImagemUrl = $"{Request.Scheme}://{Request.Host}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}",
                        Destaque = p.Destaque,
                    })
                    .ToList();

                return Ok(produtos);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET imagem-arquivo
        [HttpGet("imagem-arquivo/{nomeArquivo}")]
        public IActionResult ObterImagemArquivo(string nomeArquivo)
        {
            var pastaImagens = Path.Combine("C:", "IMAGENS_PRODUTOS");
            var caminhoCompleto = Path.Combine(pastaImagens, nomeArquivo);

            if (!System.IO.File.Exists(caminhoCompleto))
                return NotFound();

            var contentType = "image/png"; // você pode detectar dinamicamente se quiser

            return PhysicalFile(caminhoCompleto, contentType);
        }

        #endregion

        #region GET by ID Detalhes
        [HttpGet("obter-por-id-detalhes/{id}")]
        public async Task<ActionResult<ProdutoDetalhesDto>> ObterProdutoPorIdDetalhes(int id)
        {
            try
            {
                // Configura o parâmetro SQL
                SqlParameter param = new SqlParameter("@ProdutoID", id);

                // Executa o SQL diretamente com ExecuteSqlRawAsync
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "EXEC ObterProdutoPorIDDetalhes @ProdutoID";
                    command.Parameters.Add(param);

                    // Abre a conexão
                    await _context.Database.GetDbConnection().OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Mapeia os dados do produto para o DTO
                            ProdutoDetalhesDto dto = new ProdutoDetalhesDto
                            {
                                ProdutoID = reader.GetInt32(reader.GetOrdinal("ProdutoID")),
                                Nome = reader.GetString(reader.GetOrdinal("Nome")),
                                Descricao = reader.GetString(reader.GetOrdinal("Descricao")),
                                Preco = reader.GetDecimal(reader.GetOrdinal("Preco")),
                                Estoque = reader.GetInt32(reader.GetOrdinal("Estoque")),
                                DataCadastro = reader.GetDateTime(reader.GetOrdinal("DataCadastro")),
                                ImagemUrl = $"{Request.Scheme}://{Request.Host}/api/produto/imagem-arquivo/{Path.GetFileName(reader.GetString(reader.GetOrdinal("ImagemPrincipal")))}",
                                TamanhosDisponiveis = reader.IsDBNull(reader.GetOrdinal("TamanhosDisponiveis")) ? null :
                                    reader.GetString(reader.GetOrdinal("TamanhosDisponiveis"))
                                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(t => t.Trim().ToUpper())
                                        .ToList()
                            };

                            return Ok(dto);
                        }
                    }
                }

                return NotFound("Produto não encontrado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        #endregion

        #region GET Imagens por Produto
        [HttpGet("{produtoId}/imagens")]
        public async Task<ActionResult<IEnumerable<object>>> ObterImagensPorProduto(int produtoId)
        {
            try
            {
                var imagens = await _context.ImagemProdutos
                    .Where(i => i.ProdutoID == produtoId)
                    .Select(i => new
                    {
                        Url = $"{Request.Scheme}://{Request.Host}/api/produto/imagem-arquivo/{Path.GetFileName(i.Imagem)}",
                        i.ImagemPrincipal
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(imagens);
            }
            catch
            {
                return StatusCode(500, "Erro ao carregar imagens do produto.");
            }
        }
        #endregion

        #region POST Obter por IDs
        [HttpPost("obter-por-ids")]
        public async Task<ActionResult<IEnumerable<ProdutoDto>>> ObterProdutosPorIds([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return BadRequest("Lista de IDs está vazia.");

                // Constrói uma string com os IDs separados por vírgula para passar como parâmetro
                string idsConcatenados = string.Join(",", ids);

                var param = new SqlParameter("@Ids", idsConcatenados);

                List<Produto> produtosRaw = await _context.Produtos
                    .FromSqlRaw("EXEC ObterProdutosPorIds @Ids", param)
                    .AsNoTracking()
                    .ToListAsync();

                List<ProdutoDto> produtos = produtosRaw
                    .Select(p => new ProdutoDto
                    {
                        ProdutoID = p.ProdutoID,
                        Nome = p.Nome,
                        Descricao = p.Descricao,
                        Preco = p.Preco,
                        Estoque = p.Estoque,
                        DataCadastro = p.DataCadastro,
                        Categorias = p.Categorias, // As categorias como string separada por ";"
                        ImagemUrl = $"{Request.Scheme}://{Request.Host}/api/produto/imagem-arquivo/{Path.GetFileName(p.ImagemPrincipal)}"
                    })
                    .ToList();

                return Ok(produtos);
            }
            catch
            {
                return StatusCode(500, "Erro ao buscar produtos por IDs.");
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> ExcluirProduto(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarProduto @ProdutoID",
                    new SqlParameter("@ProdutoID", id)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Produto não encontrado para exclusão.");

                return Ok("Produto deletado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion
    }
}
