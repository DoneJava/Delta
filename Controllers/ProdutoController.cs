#region Usings
using DELTAAPI.Data;
using DELTAAPI.Service;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        private ProdutoService _ProdutoService;
        #endregion

        #region Constructor
        public ProdutoController(DeltaContext context, ProdutoService produtoService)
        {
            _context = context; 
            _ProdutoService = produtoService;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<IActionResult> ObterTodosProdutos()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var retornoDTO = await _ProdutoService.ObterTodosProdutos(baseUrl);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region GET Destaques
        [HttpGet("obter-destaques")]
        public async Task<IActionResult> ObterDestaquesProdutos()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var retornoDTO = await _ProdutoService.ObterDestaquesProdutos(baseUrl);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
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
        public async Task<IActionResult> ObterProdutoPorIdDetalhes(int id)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var retornoDTO = await _ProdutoService.ObterProdutoPorIdDetalhes(id, baseUrl);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region GET Imagens por Produto
        [HttpGet("{produtoId}/imagens")]
        public async Task<IActionResult> ObterImagensPorProduto(int produtoId)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var retornoDTO = await _ProdutoService.ObterImagensPorProduto(produtoId, baseUrl);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region POST Obter por IDs
        [HttpPost("obter-por-ids")]
        public async Task<IActionResult> ObterProdutosPorIds([FromBody] List<int> ids)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var retornoDTO = await _ProdutoService.ObterProdutosPorIds(ids, baseUrl);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? retornoDTO.Objeto ?? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> ExcluirProduto(int id)
        {
            var retornoDTO = await _ProdutoService.ExcluirProduto(id);

            return StatusCode((int)retornoDTO.Status,
                retornoDTO.Sucesso ? new { mensagem = retornoDTO.Mensagem }
                                   : new { sucesso = false, mensagem = retornoDTO.Mensagem });
        }
        #endregion
    }
}
