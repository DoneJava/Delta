using System.Data;
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    /// <summary>
    /// Serviço de consulta de versículos, delegando parsing/normalização
    /// para a stored procedure dbo.usp_BuscarVersiculos.
    /// </summary>
    public class VersiculosService
    {
        #region Fields
        private readonly DeltaContext _context;
        #endregion

        #region Ctor
        public VersiculosService(DeltaContext context)
        {
            _context = context;
        }
        #endregion

        #region Públicos

        /// <summary>
        /// Lista livros existentes na tabela Versiculos (distintos, ordenados).
        /// </summary>
        public async Task<List<string>> ListarLivrosAsync(CancellationToken ct = default)
        {
            return await _context.Set<Versiculos>()
                .AsNoTracking()
                .Where(v => v.Livro != null && v.Livro != "")
                .Select(v => v.Livro)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync(ct);
        }

        /// <summary>
        /// Aceita "Jo 3:16-18,21" | "Salmos 23" | "Salmos"
        /// Toda a lógica de parse/alias fica a cargo da proc.
        /// </summary>
        public Task<List<VersoDto>> BuscarPorReferenciaAsync(string referencia, CancellationToken ct = default)
        {
            var refTrim = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
            return ExecProcAsync(refTrim, null, null, null, ct);
        }

        /// <summary>
        /// Busca por campos. 'versoExpr' pode ser "16" | "16-18,21".
        /// Deixe null para capítulo inteiro; deixe capitulo null para livro inteiro.
        /// </summary>
        public Task<List<VersoDto>> BuscarAsync(
            string livro,
            int? capitulo,
            string? versoExpr = null,
            CancellationToken ct = default)
        {
            var livroParam = string.IsNullOrWhiteSpace(livro) ? null : livro.Trim();

            // normaliza expressão de versos (a proc também lida, mas ajudamos aqui)
            string? versosParam = null;
            if (!string.IsNullOrWhiteSpace(versoExpr))
            {
                versosParam = versoExpr.Trim()
                                       .Replace(" ", string.Empty)
                                       .Replace("；", ";")   // ponto-e-vírgula fullwidth
                                       .Replace(";", ",");
            }

            return ExecProcAsync(null, livroParam, capitulo, versosParam, ct);
        }

        #endregion

        #region Internos

        private async Task<List<VersoDto>> ExecProcAsync(
            string? refStr,
            string? livro,
            int? capitulo,
            string? versos,
            CancellationToken ct)
        {
            var result = new List<VersoDto>();

            // Usa a própria conexão do EF Core
            var conn = _context.Database.GetDbConnection();

            // Abrimos se necessário
            var mustClose = conn.State != ConnectionState.Open;
            if (mustClose)
                await _context.Database.OpenConnectionAsync(ct);

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.usp_BuscarVersiculos";
                cmd.CommandType = CommandType.StoredProcedure;

                // Parâmetros da proc (todos opcionais)
                var pRef = new SqlParameter("@Ref", SqlDbType.NVarChar, 120)
                {
                    Value = (object?)refStr ?? DBNull.Value
                };
                var pLivro = new SqlParameter("@Livro", SqlDbType.NVarChar, 100)
                {
                    Value = (object?)livro ?? DBNull.Value
                };
                var pCap = new SqlParameter("@Capitulo", SqlDbType.Int)
                {
                    Value = (object?)capitulo ?? DBNull.Value
                };
                var pVersos = new SqlParameter("@Versos", SqlDbType.NVarChar, 100)
                {
                    Value = (object?)versos ?? DBNull.Value
                };

                cmd.Parameters.AddRange(new[] { pRef, pLivro, pCap, pVersos });

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var ordVersiculoId = reader.GetOrdinal("VersiculoId");
                var ordLivro = reader.GetOrdinal("Livro");
                var ordCapitulo = reader.GetOrdinal("Capitulo");
                var ordVersiculo = reader.GetOrdinal("Versiculo");
                var ordTexto = reader.GetOrdinal("Texto");

                while (await reader.ReadAsync(ct))
                {
                    result.Add(new VersoDto
                    {
                        VersiculoId = !reader.IsDBNull(ordVersiculoId) ? reader.GetInt32(ordVersiculoId) : 0,
                        Livro = !reader.IsDBNull(ordLivro) ? reader.GetString(ordLivro) : "",
                        Capitulo = !reader.IsDBNull(ordCapitulo) ? reader.GetInt32(ordCapitulo) : 0,
                        Versiculo = !reader.IsDBNull(ordVersiculo) ? reader.GetInt32(ordVersiculo) : 0,
                        Texto = !reader.IsDBNull(ordTexto) ? reader.GetString(ordTexto) : ""
                    });
                }
            }
            finally
            {
                if (mustClose)
                    await _context.Database.CloseConnectionAsync();
            }

            return result;
        }

        #endregion
    }
}
