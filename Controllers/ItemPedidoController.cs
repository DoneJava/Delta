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
    public class ItemPedidoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Constructor
        public ItemPedidoController(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<ItemPedidoDto>>> ObterTodosItensPedido()
        {
            try
            {
                List<ItemPedido> itensPedido = await _context.ItensPedido
                    .FromSqlRaw("EXEC ListarItensPedido")
                    .AsNoTracking()
                    .ToListAsync();

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

                return Ok(itensDto);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<ActionResult<ItemPedidoDto>> ObterItemPedidoPorId(int id)
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
                    return NotFound("Item de pedido não encontrado.");

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

                return Ok(dto);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST
        [HttpPost("inserir")]
        public async Task<IActionResult> InserirItemPedido([FromBody] ItemPedidoCreateDto dto)
        {
            try
            {
                bool pedidoExiste = await _context.Pedidos
                    .AsNoTracking()
                    .AnyAsync(p => p.PedidoID == dto.PedidoID);

                if (!pedidoExiste)
                    return BadRequest($"Pedido com ID {dto.PedidoID} não encontrado.");

                bool produtoExiste = await _context.Produtos
                    .AsNoTracking()
                    .AnyAsync(p => p.ProdutoID == dto.ProdutoID);

                if (!produtoExiste)
                    return BadRequest($"Produto com ID {dto.ProdutoID} não encontrado.");

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirItemPedido @PedidoID, @ProdutoID, @Quantidade, @PrecoUnitario",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@ProdutoID", dto.ProdutoID),
                    new SqlParameter("@Quantidade", dto.Quantidade),
                    new SqlParameter("@PrecoUnitario", dto.PrecoUnitario)
                );

                return Ok("Item de pedido inserido com sucesso.");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Erro de banco de dados: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Erro de atualização no banco: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region PUT
        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarItemPedido(int id, [FromBody] ItemPedidoUpdateDto dto)
        {
            if (id != dto.ItemPedidoID)
                return BadRequest("ID inconsistente.");

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
                    return NotFound("Item de pedido não encontrado para atualização.");

                return Ok("Item de pedido atualizado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region DELETE
        [HttpDelete("deletar/{id}")]
        public async Task<IActionResult> DeletarItemPedido(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarItemPedido @ItemPedidoID",
                    new SqlParameter("@ItemPedidoID", id)
                );

                if (linhasAfetadas == 0)
                    return NotFound("Item de pedido não encontrado para exclusão.");

                return Ok("Item de pedido deletado com sucesso.");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion
    }
}
