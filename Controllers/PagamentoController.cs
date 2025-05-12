#region Usings
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Enums;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
#endregion

namespace DELTAAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentoController : ControllerBase
    {
        #region Fields
        private readonly DeltaContext _context;
        private readonly ILogger<PagamentoController> _logger;
        private const string _clientId = "18508";
        private const string _clientSecret = "43tLAiXZxoisr2acEg6GoGeZCPgaJ2iNclSNIr6d";
        private const string _redirectUri = "https://41e7-2804-14d-5c21-a0c2-64a9-b425-2a61-e640.ngrok-free.app/api/pagamento/melhorenvio/retorno";
        #endregion

        #region Constructor
        public PagamentoController(DeltaContext context, ILogger<PagamentoController> logger)
        {
            _context = context;
            _logger = logger;
        }
        #endregion

        #region GET All
        [HttpGet("obter-todos")]
        public async Task<ActionResult<IEnumerable<PagamentoDto>>> ObterTodosPagamentos()
        {
            try
            {
                List<Pagamento> pagamentosRaw = await _context.Pagamentos
                    .FromSqlRaw("EXEC ListarPagamentos")
                    .AsNoTracking()
                    .ToListAsync();

                List<PagamentoDto> pagamentos = pagamentosRaw
                    .Select(p => new PagamentoDto
                    {
                        PagamentoID = p.PagamentoID,
                        PedidoID = p.PedidoID,
                        ValorPago = p.ValorPago,
                        MetodoPagamento = p.MetodoPagamento,
                        StatusPagamento = p.StatusPagamento,
                        DataPagamento = p.DataPagamento
                    })
                    .ToList();

                return Ok(pagamentos);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region GET by ID
        [HttpGet("obter-por-id/{id}")]
        public async Task<ActionResult<PagamentoDto>> ObterPagamentoPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@PagamentoID", id);

                Pagamento? pagamento = await Task.Run(() =>
                    _context.Pagamentos
                        .FromSqlRaw("EXEC ObterPagamentoPorID @PagamentoID", param)
                        .AsNoTracking()
                        .AsEnumerable()
                        .FirstOrDefault());

                if (pagamento == null)
                    return NotFound("Pagamento não encontrado.");

                PagamentoDto dto = new PagamentoDto
                {
                    PagamentoID = pagamento.PagamentoID,
                    PedidoID = pagamento.PedidoID,
                    ValorPago = pagamento.ValorPago,
                    MetodoPagamento = pagamento.MetodoPagamento,
                    StatusPagamento = pagamento.StatusPagamento,
                    DataPagamento = pagamento.DataPagamento
                };

                return Ok(dto);
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region POST Registrar Pagamento
        [HttpPost("registrar-pagamento")]
        public async Task<IActionResult> RegistrarPagamento([FromBody] PagamenDadosPagamentoDtotoDTO dto)
        {
            try
            {
                // Definir a conexão com o banco
                using (SqlConnection connection = new SqlConnection("SuaConnectionStringAqui"))
                {
                    await connection.OpenAsync();

                    // Definindo a consulta para executar a procedure no banco de dados
                    using (SqlCommand command = new SqlCommand("RegistrarPagamento", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        // Adicionando parâmetros à consulta (mapeando dados do DTO)
                        command.Parameters.AddWithValue("@Produtos", dto.Produtos);
                        command.Parameters.AddWithValue("@MetodoPagamento", dto.MetodoPagamento);
                        command.Parameters.AddWithValue("@DadosPagamento", dto.DadosPagamento);
                        command.Parameters.AddWithValue("@DadosEnvio", dto.DadosEnvio);
                        command.Parameters.AddWithValue("@Cupom", dto.Cupom);

                        // Parâmetros para capturar o retorno da procedure
                        SqlParameter sucessoParam = new SqlParameter("@Sucesso", System.Data.SqlDbType.Bit)
                        {
                            Direction = System.Data.ParameterDirection.Output
                        };
                        command.Parameters.Add(sucessoParam);

                        SqlParameter mensagemParam = new SqlParameter("@Mensagem", System.Data.SqlDbType.NVarChar, 500)
                        {
                            Direction = System.Data.ParameterDirection.Output
                        };
                        command.Parameters.Add(mensagemParam);

                        // Executando a procedure
                        await command.ExecuteNonQueryAsync();

                        // Verificando se a operação foi bem-sucedida
                        bool sucesso = Convert.ToBoolean(sucessoParam.Value);
                        string? mensagem = mensagemParam.Value.ToString();

                        if (!sucesso)
                        {
                            // Se o sucesso for false, retornamos a mensagem de erro
                            return StatusCode(400, new { sucesso = false, mensagem });
                        }
                    }
                }

                // Se tudo ocorrer bem, retorna um status 200 com a mensagem de sucesso
                return Ok(new { sucesso = true, mensagem = "Pagamento registrado com sucesso!" });
            }
            catch (Exception)
            {
                // Tratamento genérico de erro
                return StatusCode(500, new { sucesso = false, mensagem = "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!" });
            }
        }
        #endregion

        #region POST - Validar Cupom
        [HttpPost("validarCupom")]
        public async Task<IActionResult> ValidarCupom([FromBody] CupomDto dto)
        {
            try
            {
                CupomResultadoDto cupomValidado = await ValidarCupom(dto.Codigo);
                return Ok(cupomValidado);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Erro ao validar cupom: {ex.Message}");
            }
            catch
            {
                return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            }
        }
        #endregion

        #region Calcular valor total pagamento
        [HttpPost("calcular_valor_total")]
        public async Task<IActionResult> CalcularValorTotal([FromBody] DadosCalculoValorDto dto)
        {
            try
            {
                #region Log: Início da operação
                _logger.LogInformation("Iniciando cálculo de valor total. Cupom: {Cupom}, Produtos: {QtdProdutos}", dto.Cupom, dto.Produtos?.Count ?? 0);
                #endregion

                #region Validação inicial do DTO

                if (dto == null || dto.Produtos == null || dto.Produtos.Count == 0)
                {
                    _logger.LogWarning("Requisição inválida: produtos nulos ou vazios.");
                    return BadRequest("Dados inválidos para cálculo.");
                }

                List<int> idsUnicosValidos = dto.Produtos
                    .Where(p => p.IdProduto > 0)
                    .Select(p => p.IdProduto)
                    .Distinct()
                    .ToList();

                if (idsUnicosValidos.Count == 0)
                {
                    _logger.LogWarning("IDs de produtos inválidos.");
                    return BadRequest("Nenhum produto válido informado.");
                }

                #endregion

                #region Obtenção dos produtos do banco

                string idsProdutosConcatenados = string.Join(",", idsUnicosValidos);
                SqlParameter paramIds = new SqlParameter("@Ids", idsProdutosConcatenados);

                List<Produto> produtosDoBanco = await _context.Produtos
                    .FromSqlRaw("EXEC ObterProdutosPorIds @Ids", paramIds)
                    .AsNoTracking()
                    .ToListAsync();

                if (produtosDoBanco == null || produtosDoBanco.Count == 0)
                {
                    _logger.LogWarning("Produtos não encontrados no banco. IDs: {Ids}", idsProdutosConcatenados);
                    return BadRequest("Produtos não encontrados.");
                }

                #endregion

                #region Validação do cupom (opcional)

                CupomResultadoDto cupomResultado = new CupomResultadoDto();

                if (!string.IsNullOrWhiteSpace(dto.Cupom))
                {
                    CupomResultadoDto resultado = await ValidarCupom(dto.Cupom);
                    cupomResultado = resultado.Valido ? resultado : new CupomResultadoDto();

                    _logger.LogInformation("Cupom processado. Código: {Cupom}, Válido: {Valido}, %: {Pct}, Valor: {Val}",
                        dto.Cupom, resultado.Valido, resultado.DescontoPorcentagem, resultado.DescontoValor);
                }

                #endregion

                #region Cálculo do valor total

                decimal valorTotal = 0;

                foreach (DadosProdutoDto produtoDto in dto.Produtos)
                {
                    if (produtoDto.Quantidade <= 0)
                    {
                        _logger.LogWarning("Produto com quantidade inválida: ID {IdProduto}, Quantidade {Qtd}", produtoDto.IdProduto, produtoDto.Quantidade);
                        continue;
                    }

                    Produto? produtoBanco = produtosDoBanco.FirstOrDefault(p => p.ProdutoID == produtoDto.IdProduto);
                    if (produtoBanco != null)
                    {
                        decimal subtotal = produtoBanco.Preco * produtoDto.Quantidade;
                        valorTotal += subtotal;

                        _logger.LogInformation("Produto calculado: ID {Id}, Preço: {Preco}, Qtd: {Qtd}, Subtotal: {Subtotal}",
                            produtoBanco.ProdutoID, produtoBanco.Preco, produtoDto.Quantidade, subtotal);
                    }
                    else
                    {
                        _logger.LogWarning("Produto não encontrado no banco: ID {IdProduto}", produtoDto.IdProduto);
                    }
                }

                #endregion

                #region Aplicação do desconto

                decimal desconto = 0;

                if (cupomResultado.DescontoPorcentagem > 0)
                {
                    desconto = valorTotal * (cupomResultado.DescontoPorcentagem / 100M);
                }
                else if (cupomResultado.DescontoValor > 0)
                {
                    desconto = cupomResultado.DescontoValor;
                }

                decimal valorFinal = valorTotal - desconto;
                if (valorFinal < 0)
                {
                    valorFinal = 0;
                }

                _logger.LogInformation("Valor total bruto: {Bruto}, Desconto: {Desc}, Valor final: {Final}", valorTotal, desconto, valorFinal);

                #endregion

                #region Retorno do valor final

                // Arredondamento final com regra bancária
                valorFinal = Math.Round(valorFinal, 2, MidpointRounding.AwayFromZero);

                // Proteções contra entradas anormais
                if (valorFinal > 1_000_000)
                {
                    _logger.LogWarning("Valor final extremamente alto: {Valor}", valorFinal);
                    return BadRequest("Valor fora do limite permitido.");
                }

                ValorPagamentoDto valorPagamento = new ValorPagamentoDto
                {
                    valorTotal = valorFinal
                };

                return Ok(valorPagamento);

                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao calcular valor total.");
                return StatusCode(500, "Ocorreu um erro interno ao calcular o valor total.");
            }
        }
        #endregion

        #region Método para obter token do Melhor Envio
        private async Task<string?> RenovarTokenMelhorEnvioAsync(string refreshToken)
        {
            using HttpClient client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            HttpResponseMessage response = await client.PostAsync("https://melhorenvio.com.br/oauth/token", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao renovar token: {Status}", response.StatusCode);
                return null;
            }

            string result = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(result)!;

            var novo = new TokenMelhorEnvio
            {
                AccessToken = data.access_token,
                RefreshToken = data.refresh_token,
                Expiracao = DateTime.UtcNow.AddSeconds((int)data.expires_in - 60),
                AtualizadoEm = DateTime.UtcNow,
                CriadoEm = DateTime.UtcNow
            };

            _context.TokenMelhorEnvio.Add(novo);
            await _context.SaveChangesAsync();

            return novo.AccessToken;
        }
        private async Task<string?> ObterTokenMelhorEnvioAsync()
        {
            TokenMelhorEnvio? tokenAtual = await _context.TokenMelhorEnvio
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (tokenAtual != null)
            {
                if (tokenAtual.Expiracao > DateTime.UtcNow)
                    return tokenAtual.AccessToken;

                if (!string.IsNullOrEmpty(tokenAtual.RefreshToken))
                    return await RenovarTokenMelhorEnvioAsync(tokenAtual.RefreshToken);
            }

            return null; // Token não existe ainda
        }

        [HttpGet("melhorenvio/retorno")]
        public async Task<IActionResult> RetornoMelhorEnvio([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Código de autorização não fornecido.");

            using HttpClient client = new HttpClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                new KeyValuePair<string, string>("code", code)
            });

            _logger.LogInformation("Enviando para token: grant_type=authorization_code, client_id={id}, redirect_uri={uri}, code={code}", _clientId, _redirectUri, code);

            HttpResponseMessage response = await client.PostAsync("https://www.melhorenvio.com.br/oauth/token", content);
            _logger.LogError("Status: {Status}, Reason: {Reason}, Body: {Body}", response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync());




            if (!response.IsSuccessStatusCode)
            {
                string erro = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao trocar authorization code por token: {erro}", erro);
                return StatusCode(500, "Falha ao obter token do Melhor Envio.");
            }

            string result = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(result)!;

            var token = new TokenMelhorEnvio
            {
                AccessToken = data.access_token,
                RefreshToken = data.refresh_token,
                Expiracao = DateTime.UtcNow.AddSeconds((int)data.expires_in - 60),
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _context.TokenMelhorEnvio.Add(token);
            await _context.SaveChangesAsync();

            return Ok("Token do Melhor Envio obtido e armazenado com sucesso.");
        }
        #endregion

        #region Calcular Frete
        [HttpPost("calcular-frete")]
        public async Task<IActionResult> CalcularFrete([FromBody] FreteCalculoDto dto)
        {
            try
            {
                string? cepDestino = dto?.Cep;

                if (string.IsNullOrWhiteSpace(cepDestino))
                {
                    string? token = HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                    if (string.IsNullOrWhiteSpace(token) || token == "null")
                        return BadRequest("Token ausente e nenhum CEP informado.");

                    Cliente? usuario = await Task.Run(() =>
                        _context.Clientes
                            .FromSqlRaw("EXEC ObterClientePorToken @Token", new SqlParameter("@Token", token))
                            .AsEnumerable()
                            .SingleOrDefault()
                    );

                    if (usuario == null || string.IsNullOrWhiteSpace(usuario.CEP))
                        return BadRequest("Usuário inválido ou sem CEP cadastrado.");

                    cepDestino = usuario.CEP;
                }

                string? tokenMelhorEnvio = await ObterTokenMelhorEnvioAsync();
                if (string.IsNullOrWhiteSpace(tokenMelhorEnvio))
                    return StatusCode(401, "Token do Melhor Envio não disponível. Autenticação falhou.");

                string cepOrigem = "20560031"; // CEP fixo de origem (sua casa)

                using HttpClient http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenMelhorEnvio);

                var payload = new
                {
                    from = new { postal_code = cepOrigem },
                    to = new { postal_code = cepDestino },
                    products = new[]
                    {
                        new
                        {
                            width = 15,
                            height = 5,
                            length = 20,
                            weight = 1.0,
                            insurance_value = 0,
                            quantity = 1
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await http.PostAsync("https://melhorenvio.com.br/api/v2/me/shipment/calculate", content);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Erro ao consultar Melhor Envio.");

                string respostaJson = await response.Content.ReadAsStringAsync();

                JObject servico = JObject.Parse(respostaJson);

                List<FreteResultadoDto> resultados = new List<FreteResultadoDto>
{
                    new FreteResultadoDto
                    {
                        Transportadora = servico["company"]?["name"]?.ToString() ?? "Desconhecida",
                        Valor = decimal.Parse(servico["price"]?.ToString() ?? "0", CultureInfo.InvariantCulture),
                        PrazoEntrega = int.Parse(servico["delivery_time"]?.ToString() ?? "0", CultureInfo.InvariantCulture)
                    }
                };


                return Ok(resultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular frete com o Melhor Envio.");
                return StatusCode(500, "Erro interno ao calcular o frete.");
            }
        }

        private async Task SalvarTokenNaBase(string accessToken, string refreshToken, DateTime expiracao)
        {
            TokenMelhorEnvio novo = new TokenMelhorEnvio
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expiracao = expiracao,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow
            };

            _context.TokenMelhorEnvio.Add(novo);
            await _context.SaveChangesAsync();
        }

        private async Task<TokenMelhorEnvio?> ObterUltimoTokenValido()
        {
            return await _context.TokenMelhorEnvio
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Processar Pagamento
        [HttpPost("processar_pagamento")]
        public async Task<IActionResult> process_payment([FromBody] dynamic dto)
        {
            return Ok();
        }
        #endregion 

        #region POST Estornar
        [HttpPost("estornar/{id}")]
        public async Task<IActionResult> EstornarPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser Estornados.");
            //try
            //{
            //    Pagamento? pagamento = await _context.Pagamentos
            //        .AsNoTracking()
            //        .FirstOrDefaultAsync(p => p.PagamentoID == id);

            //    if (pagamento == null)
            //        return NotFound("Pagamento não encontrado.");

            //    if (pagamento.StatusPagamento != StatusPagamento.Pago)
            //        return BadRequest("Somente pagamentos com status 'Pago' podem ser estornados.");

            //    await _context.Database.ExecuteSqlRawAsync(
            //        "EXEC EstornarPagamento @PagamentoID",
            //        new SqlParameter("@PagamentoID", id)
            //    );

            //    return Ok("Pagamento estornado com sucesso.");
            //}
            //catch (SqlException ex)
            //{
            //    return StatusCode(500, $"Erro ao estornar pagamento: {ex.Message}");
            //}
            //catch
            //{
            //    return StatusCode(500, "Problema com o servidor, por favor, aguarde pois já estamos resolvendo!");
            //}
        }
        #endregion

        #region PUT (bloqueado)
        [HttpPut("editar/{id}")]
        public IActionResult EditarPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser editados. Estorne ou crie um novo pagamento.");
        }
        #endregion

        #region DELETE (bloqueado)
        [HttpDelete("excluir/{id}")]
        public IActionResult ExcluirPagamento(int id)
        {
            return StatusCode(405, "Pagamentos não podem ser deletados.");
        }
        #endregion

        #region Métodos Privados
        // Valida o cupom utilizando a procedure
        private async Task<CupomResultadoDto> ValidarCupom(string cupom)
        {
            SqlParameter paramCodigo = new SqlParameter("@Codigo", cupom);

            SqlParameter paramValido = new SqlParameter
            {
                ParameterName = "@Valido",
                SqlDbType = System.Data.SqlDbType.Bit,
                Direction = System.Data.ParameterDirection.Output
            };

            SqlParameter paramPorcentagem = new SqlParameter
            {
                ParameterName = "@DescontoPorcentagem",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            SqlParameter paramValor = new SqlParameter
            {
                ParameterName = "@DescontoValor",
                SqlDbType = System.Data.SqlDbType.Decimal,
                Precision = 18,
                Scale = 2,
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC ValidarCupom @Codigo, @Valido OUTPUT, @DescontoPorcentagem OUTPUT, @DescontoValor OUTPUT",
                paramCodigo, paramValido, paramPorcentagem, paramValor
            );

            return new CupomResultadoDto
            {
                Valido = (bool)(paramValido.Value ?? false),
                DescontoPorcentagem = paramPorcentagem.Value != DBNull.Value ? (int)paramPorcentagem.Value : 0,
                DescontoValor = paramValor.Value != DBNull.Value ? (decimal)paramValor.Value : 0
            };
        }

        // Chama a procedure para obter os produtos com base nos IDs
        private async Task<List<Produto>> ObterProdutosPorIds(string ids)
        {
            var paramIds = new SqlParameter("@Ids", ids);

            var produtos = await _context.Produtos
                .FromSqlRaw("EXEC ObterProdutosPorIds @Ids", paramIds)
                .ToListAsync();

            return produtos;
        }
        #endregion
    }
}
