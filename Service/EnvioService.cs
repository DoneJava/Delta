using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    public class EnvioService
    {
        #region #Fields
        private readonly DeltaContext _context;
        #endregion

        #region Construtores
        public EnvioService(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region Métodos Públicos
        public async Task<RetornoDTO> ObterTodosEnvios()
        {
            try
            {
                List<Envio> enviosRaw = await _context.Envios
                    .FromSqlRaw("EXEC ListarEnvios")
                    .AsNoTracking()
                    .ToListAsync();

                if (enviosRaw == null || enviosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum envio encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                List<Pedido> pedidos = await _context.Pedidos
                    .AsNoTracking()
                    .ToListAsync();

                List<EnvioDto> envios = enviosRaw
                    .Join(pedidos,
                        e => e.PedidoID,
                        p => p.PedidoID,
                        (e, p) => new EnvioDto
                        {
                            EnvioID = e.EnvioID,
                            PedidoID = e.PedidoID,
                            MetodoEnvio = e.MetodoEnvio,
                            StatusEnvio = e.StatusEnvio,
                            CodigoRastreamento = e.CodigoRastreamento,
                            DataEnvio = e.DataEnvio
                        })
                    .ToList();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Envios obtidos com sucesso.",
                    Objeto = envios,
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro ao obter envios.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ObterEnvioPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@EnvioID", id);

                List<Envio> envios = await _context.Envios
                    .FromSqlRaw("EXEC ObterEnvioPorID @EnvioID", param)
                    .AsNoTracking()
                    .ToListAsync();

                Envio? envio = envios.FirstOrDefault();

                if (envio == null)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Envio não encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                EnvioDto dto = new EnvioDto
                {
                    EnvioID = envio.EnvioID,
                    PedidoID = envio.PedidoID,
                    MetodoEnvio = envio.MetodoEnvio,
                    StatusEnvio = envio.StatusEnvio,
                    CodigoRastreamento = envio.CodigoRastreamento,
                    DataEnvio = envio.DataEnvio
                };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Envio obtido com sucesso.",
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

        public async Task<RetornoDTO> CriarEnvio(EnvioCreateDto dto)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC InserirEnvio @PedidoID, @MetodoEnvio, @StatusEnvio, @CodigoRastreamento",
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@MetodoEnvio", dto.MetodoEnvio),
                    new SqlParameter("@StatusEnvio", dto.StatusEnvio),
                    new SqlParameter("@CodigoRastreamento", dto.CodigoRastreamento)
                );

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Envio criado com sucesso.",
                    Status = StatusRetorno.Created
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

        public async Task<RetornoDTO> AtualizarEnvio(int id, EnvioUpdateDto dto)
        {
            if (id != dto.EnvioID)
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
                    "EXEC AtualizarEnvio @EnvioID, @PedidoID, @MetodoEnvio, @StatusEnvio, @CodigoRastreamento",
                    new SqlParameter("@EnvioID", dto.EnvioID),
                    new SqlParameter("@PedidoID", dto.PedidoID),
                    new SqlParameter("@MetodoEnvio", dto.MetodoEnvio),
                    new SqlParameter("@StatusEnvio", dto.StatusEnvio),
                    new SqlParameter("@CodigoRastreamento", dto.CodigoRastreamento)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Envio não encontrado para atualização.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Envio atualizado com sucesso.",
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

        public async Task<RetornoDTO> DeletarEnvio(int id)
        {
            try
            {
                int linhasAfetadas = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC DeletarEnvio @EnvioID",
                    new SqlParameter("@EnvioID", id)
                );

                if (linhasAfetadas == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Envio não encontrado para exclusão.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Envio deletado com sucesso.",
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
