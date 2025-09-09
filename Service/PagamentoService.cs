using DELTAAPI.Controllers;
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Enums;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;

namespace DELTAAPI.Service
{
    public class PagamentoService
    {
        #region Fields
        private readonly DeltaContext _context;
        private readonly ILogger<PagamentoService> _logger;
        private readonly IConfiguration _config;
        #endregion

        #region Construtor
        public PagamentoService(ILogger<PagamentoService> logger, DeltaContext context, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }
        #endregion

        #region CRUD / Consultas
        private async Task<bool> AtualizarStatusPagamentoAsync(int pagamentoId, StatusPagamento novoStatus)
        {
            var pagamento = await _context.Pagamentos.FindAsync(pagamentoId);
            if (pagamento == null || pagamento.StatusPagamento == StatusPagamento.Estornado)
                return false;

            if (novoStatus == StatusPagamento.Pago && pagamento.StatusPagamento != StatusPagamento.Aguardando)
                return false;

            if (novoStatus == StatusPagamento.Recusado && pagamento.StatusPagamento != StatusPagamento.Aguardando)
                return false;

            pagamento.StatusPagamento = novoStatus;
            _context.Entry(pagamento).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<RetornoDTO> ObterTodosPagamentos()
        {
            try
            {
                List<Pagamento> pagamentosRaw = await _context.Pagamentos
                    .FromSqlRaw("EXEC ListarPagamentos")
                    .AsNoTracking()
                    .ToListAsync();

                if (pagamentosRaw == null || pagamentosRaw.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum pagamento encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

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

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pagamentos obtidos com sucesso.",
                    Objeto = pagamentos,
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

        public async Task<RetornoDTO> ObterPagamentoPorId(int id)
        {
            try
            {
                SqlParameter param = new SqlParameter("@PagamentoID", id);

                Pagamento? pagamento = await _context.Pagamentos
                    .FromSqlRaw("EXEC ObterPagamentoPorID @PagamentoID", param)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (pagamento == null)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Pagamento não encontrado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                PagamentoDto dto = new PagamentoDto
                {
                    PagamentoID = pagamento.PagamentoID,
                    PedidoID = pagamento.PedidoID,
                    ValorPago = pagamento.ValorPago,
                    MetodoPagamento = pagamento.MetodoPagamento,
                    StatusPagamento = pagamento.StatusPagamento,
                    DataPagamento = pagamento.DataPagamento
                };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pagamento obtido com sucesso.",
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

        public async Task<RetornoDTO> ValidarCupom(string codigo)
        {
            try
            {
                SqlParameter paramCodigo = new SqlParameter("@Codigo", codigo);

                SqlParameter paramValido = new SqlParameter
                {
                    ParameterName = "@Valido",
                    SqlDbType = SqlDbType.Bit,
                    Direction = ParameterDirection.Output
                };

                SqlParameter paramPorcentagem = new SqlParameter
                {
                    ParameterName = "@DescontoPorcentagem",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };

                SqlParameter paramFreteGratis = new SqlParameter
                {
                    ParameterName = "@FreteGratis",
                    SqlDbType = SqlDbType.Bit,
                    Direction = ParameterDirection.Output
                };

                SqlParameter paramValor = new SqlParameter
                {
                    ParameterName = "@DescontoValor",
                    SqlDbType = SqlDbType.Decimal,
                    Precision = 18,
                    Scale = 2,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ValidarCupom @Codigo, @Valido OUTPUT, @DescontoPorcentagem OUTPUT, @DescontoValor OUTPUT, @FreteGratis OUTPUT",
                    paramCodigo, paramValido, paramPorcentagem, paramValor, paramFreteGratis
                );

                var resultado = new CupomResultadoDto
                {
                    Valido = (bool)(paramValido.Value ?? false),
                    DescontoPorcentagem = paramPorcentagem.Value != DBNull.Value ? (int)paramPorcentagem.Value : 0,
                    DescontoValor = paramValor.Value != DBNull.Value ? (decimal)paramValor.Value : 0,
                    FreteGratis = paramFreteGratis.Value != DBNull.Value ? (bool)paramFreteGratis.Value : false
                };

                if (!resultado.Valido)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Cupom inválido ou expirado.",
                        Status = StatusRetorno.NotFound
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Cupom válido.",
                    Objeto = resultado,
                    Status = StatusRetorno.OK
                };
            }
            catch (SqlException ex)
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao validar cupom: {ex.Message}",
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
        #endregion

        #region Cálculo
        public async Task<RetornoDTO> CalcularValorTotal(DadosCalculoValorDto dto, string? tokenStr)
        {
            try
            {
                if (dto == null || dto.Produtos == null || dto.Produtos.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Dados inválidos para cálculo.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                List<int> idsUnicosValidos = dto.Produtos
                    .Where(p => p.IdProduto > 0)
                    .Select(p => p.IdProduto)
                    .Distinct()
                    .ToList();

                if (idsUnicosValidos.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum produto válido informado.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                string idsProdutosConcatenados = string.Join(",", idsUnicosValidos);
                SqlParameter paramIds = new SqlParameter("@Ids", idsProdutosConcatenados);

                List<Produto> produtosDoBanco = await _context.Produtos
                    .FromSqlRaw("EXEC ObterProdutosPorIds @Ids", paramIds)
                    .AsNoTracking()
                    .ToListAsync();

                if (produtosDoBanco == null || produtosDoBanco.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Produtos não encontrados.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                CupomResultadoDto cupomResultado = new CupomResultadoDto();

                if (!string.IsNullOrWhiteSpace(dto.Cupom))
                {
                    var retornoCupom = await ValidarCupom(dto.Cupom);
                    if (retornoCupom.Sucesso && retornoCupom.Objeto is CupomResultadoDto cupomOk)
                        cupomResultado = cupomOk;
                }

                decimal valorTotal = 0;

                foreach (var produtoDto in dto.Produtos)
                {
                    if (produtoDto.Quantidade <= 0) continue;

                    var produtoBanco = produtosDoBanco.FirstOrDefault(p => p.ProdutoID == produtoDto.IdProduto);
                    if (produtoBanco != null)
                    {
                        decimal subtotal = produtoBanco.Preco * produtoDto.Quantidade;
                        valorTotal += subtotal;
                    }
                }

                decimal desconto = 0;
                if (cupomResultado.DescontoPorcentagem > 0)
                    desconto = valorTotal * (cupomResultado.DescontoPorcentagem / 100M);
                else if (cupomResultado.DescontoValor > 0)
                    desconto = cupomResultado.DescontoValor;

                decimal valorFinal = valorTotal - desconto;
                if (valorFinal < 0) valorFinal = 0;

                // CEP do DTO ou do usuário (se tiver token)
                string? cep = dto.Cep;

                if (string.IsNullOrWhiteSpace(cep) && !string.IsNullOrWhiteSpace(tokenStr))
                {
                    if (Guid.TryParse(tokenStr, out var tokenGuid))
                    {
                        var p = new SqlParameter("@Token", SqlDbType.UniqueIdentifier) { Value = tokenGuid };

                        // materializa e pega o primeiro
                        var lista = await _context.Clientes
                            .FromSqlRaw("EXEC ObterClientePorToken @Token", p)
                            .AsNoTracking()
                            .ToListAsync();

                        var usuario = lista.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(usuario?.CEP))
                            cep = usuario.CEP!;
                    }
                }

                if (!string.IsNullOrWhiteSpace(cep))
                {
                    var produtosFrete = dto.Produtos?.Select(p => new ProdutoFreteDto
                    {
                        IdProduto = p.IdProduto,
                        Quantidade = p.Quantidade,
                        Tamanho = p.Tamanho ?? "-"
                    }).ToList() ?? new();

                    var retornoFrete = await CalcularFrete(new FreteCalculoDto
                    {
                        Cep = cep,
                        Produtos = produtosFrete,
                        Cupom = dto.Cupom
                    });

                    if (retornoFrete.Sucesso && retornoFrete.Objeto is List<FreteResultadoDto> fretes && fretes.Any())
                    {
                        decimal valorFrete = (fretes.First().Valor ?? 0);
                        valorFinal += valorFrete;
                    }
                }

                valorFinal = Math.Round(valorFinal, 2, MidpointRounding.AwayFromZero);

                if (valorFinal > 1_000_000)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Valor fora do limite permitido.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Cálculo realizado com sucesso.",
                    Objeto = new ValorPagamentoDto { valorTotal = valorFinal },
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular valor total.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao calcular o valor total.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }
        #endregion

        #region Processar Pagamento (PIX/teste/guest)
        public async Task<RetornoDTO> ProcessarPagamento(PagamentoProcessarDto dto, string? tokenStr)
        {
            try
            {
                int? clienteId = null;

                // cliente via token (se houver)
                if (!string.IsNullOrWhiteSpace(tokenStr) && tokenStr != "null")
                {
                    if (!Guid.TryParse(tokenStr, out var tokenGuid))
                    {
                        _logger.LogError("Token inválido em ProcessarPagamento.");
                        return new RetornoDTO
                        {
                            Sucesso = false,
                            Mensagem = "Erro interno ao processar o pagamento. Token inválido.",
                            Status = StatusRetorno.InternalServerError
                        };
                    }

                    var pToken = new SqlParameter("@Token", SqlDbType.UniqueIdentifier) { Value = tokenGuid };
                    var lista = await _context.Clientes
                        .FromSqlRaw("EXEC ObterClientePorToken @Token", pToken)
                        .AsNoTracking()
                        .ToListAsync();

                    var cliente = lista.FirstOrDefault();
                    if (cliente != null) clienteId = cliente.ClienteID;
                }

                if (dto?.Produtos == null || dto.Produtos.Count == 0)
                    return new RetornoDTO { Sucesso = false, Mensagem = "Nenhum produto informado.", Status = StatusRetorno.BadRequest };

                // OBRIGATÓRIO: id do pagamento no gateway (PIX ou cartão)
                if (string.IsNullOrWhiteSpace(dto.GatewayPaymentId))
                    return new RetornoDTO { Sucesso = false, Mensagem = "gatewayPaymentId obrigatório.", Status = StatusRetorno.BadRequest };

                // Valida no Mercado Pago se está aprovado
                //var (aprovado, status, raw) = await VerificarPagamentoAprovadoAsync(dto.GatewayPaymentId);
                //if (!aprovado)
                //{
                //    var msg = string.IsNullOrEmpty(status)
                //        ? "Não foi possível validar o pagamento junto ao gateway."
                //        : $"Pagamento não aprovado (status: {status}).";

                //    _logger.LogWarning("processar_pagamento: gatewayPaymentId={Id}, status={Status}, raw={Raw}",
                //        dto.GatewayPaymentId, status, raw);

                //    return new RetornoDTO { Sucesso = false, Mensagem = msg, Status = StatusRetorno.BadRequest };
                //}
                /// DESCOMENTAR

                // normaliza CEP (opcional)
                string cep = (dto.Cep ?? "");
                if (string.IsNullOrWhiteSpace(cep) && !string.IsNullOrWhiteSpace(dto.DadosEnvio))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(dto.DadosEnvio);
                        if (doc.RootElement.TryGetProperty("cep", out var cepEl) && cepEl.ValueKind == JsonValueKind.String)
                            cep = cepEl.GetString() ?? "";
                        else if (doc.RootElement.TryGetProperty("endereco", out var endEl)
                            && endEl.ValueKind == JsonValueKind.Object
                            && endEl.TryGetProperty("cep", out var cepEl2)
                            && cepEl2.ValueKind == JsonValueKind.String)
                            cep = cepEl2.GetString() ?? "";
                    }
                    catch { /* segue sem travar */ }
                }
                cep = System.Text.RegularExpressions.Regex.Replace(cep, @"\D", "");

                // chama a SP — frete/total são recalculados lá
                var paramProdutos = new SqlParameter("@Produtos", Newtonsoft.Json.JsonConvert.SerializeObject(dto.Produtos));
                var paramMetodo = new SqlParameter("@MetodoPagamento", dto.MetodoPagamento);
                var paramEnvio = new SqlParameter("@DadosEnvio", (object)(dto.DadosEnvio ?? "") ?? DBNull.Value);
                var paramCupom = new SqlParameter("@Cupom",
                    (object)(dto.Cupom ?? string.Empty).ToString().Trim() ?? DBNull.Value);
                var paramCliente = new SqlParameter("@ClienteID", (object?)clienteId ?? DBNull.Value);
                var paramFrete = new SqlParameter("@ValorFrete", SqlDbType.Decimal)
                {
                    IsNullable = true,
                    Precision = 18,
                    Scale = 2,
                    Value = DBNull.Value
                };
                var paramCep = new SqlParameter("@Cep", SqlDbType.VarChar, 20) { Value = (object)(cep ?? "") ?? DBNull.Value };

                // guarda o id do gateway em DadosPagamento
                var dadosPgJson = JsonSerializer.Serialize(new { gatewayPaymentId = dto.GatewayPaymentId, raw = dto.DadosPagamento });
                var paramDadosPg = new SqlParameter("@DadosPagamento", (object)dadosPgJson ?? DBNull.Value);

                var outOk = new SqlParameter("@Sucesso", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var outMsg = new SqlParameter("@Mensagem", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
                var outId = new SqlParameter("@PedidoID", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC RegistrarPagamento @Produtos, @MetodoPagamento, @DadosEnvio, @Cupom, @ClienteID, @ValorFrete, @Cep, @DadosPagamento, @Sucesso OUTPUT, @Mensagem OUTPUT, @PedidoID OUTPUT",
                    paramProdutos, paramMetodo, paramEnvio, paramCupom, paramCliente, paramFrete, paramCep, paramDadosPg,
                    outOk, outMsg, outId
                );

                bool sucesso = Convert.ToBoolean(outOk.Value);
                string mensagem = outMsg.Value?.ToString() ?? "Sem mensagem.";
                int? pedidoId = outId.Value != DBNull.Value ? Convert.ToInt32(outId.Value) : null;

                if (!sucesso)
                    return new RetornoDTO { Sucesso = false, Mensagem = mensagem, Status = StatusRetorno.BadRequest };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = mensagem,
                    Objeto = new { pedidoId = clienteId == null ? pedidoId : (int?)null }, // somente guest recebe o id
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar pagamento.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao processar o pagamento.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }
        #endregion

        #region PIX (gerar e consultar)
        public async Task<RetornoDTO> GerarPagamentoPix(DadosCalculoValorDto dto, string? tokenStr)
        {
            try
            {
                _logger.LogInformation("Iniciando geração de pagamento Pix...");

                if (dto == null || dto.Produtos == null || dto.Produtos.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Dados inválidos para gerar pagamento Pix.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                var retornoCalculo = await CalcularValorTotal(dto, tokenStr);

                if (!retornoCalculo.Sucesso || retornoCalculo.Objeto is not ValorPagamentoDto valorDto)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao calcular o valor do pagamento Pix.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                decimal? valor = valorDto.valorTotal;
                if (valor <= 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Valor inválido após cálculo para pagamento Pix.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                var token = GetMpAccessToken();

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

                string bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    transaction_amount = valor,
                    description = "Pagamento via Pix",
                    payment_method_id = "pix",
                    payer = new { email = "comprador@teste.com" } // ajuste conforme seu fluxo
                });

                HttpResponseMessage response = await client.PostAsync(
                    "https://api.mercadopago.com/v1/payments",
                    new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json")
                );

                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao gerar PIX: {Status} - {Body}", response.StatusCode, responseJson);
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao gerar pagamento Pix.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                var dados = JObject.Parse(responseJson);

                string? qrCodeBase64 = dados["point_of_interaction"]?["transaction_data"]?["qr_code_base64"]?.ToString();
                string? qrCodeText = dados["point_of_interaction"]?["transaction_data"]?["qr_code"]?.ToString();
                string? paymentId = dados["id"]?.ToString();

                if (string.IsNullOrEmpty(qrCodeBase64) || string.IsNullOrEmpty(paymentId))
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao extrair QR Code do retorno do Mercado Pago.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Pagamento Pix gerado com sucesso.",
                    Objeto = new
                    {
                        idPagamento = paymentId,
                        qrCodeBase64 = $"data:image/png;base64,{qrCodeBase64}",
                        qrCodeText = qrCodeText
                    },
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao gerar pagamento PIX.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao gerar pagamento via Pix.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }

        public async Task<RetornoDTO> ConsultarStatusPix(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "ID do pagamento não informado.",
                    Status = StatusRetorno.BadRequest
                };
            }

            try
            {
                var token = GetMpAccessToken();

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{id}");
                string respostaJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao consultar status do pagamento PIX. Status: {Status}, Resposta: {Resposta}",
                        response.StatusCode, respostaJson);

                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao consultar status do pagamento Pix.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                var dados = JObject.Parse(respostaJson);

                string? status = dados["status"]?.ToString();
                string? statusDetail = dados["status_detail"]?.ToString();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Status do pagamento PIX consultado com sucesso.",
                    Objeto = new { status = status, detalhe = statusDetail },
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao consultar status do pagamento PIX.");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao consultar status do Pix.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }
        #endregion

        #region Frete
        public async Task<RetornoDTO> CalcularFrete(FreteCalculoDto frete)
        {
            try
            {
                if (frete == null)
                {
                    return new RetornoDTO { Sucesso = false, Mensagem = "Parâmetros de frete inválidos.", Status = StatusRetorno.BadRequest };
                }

                // Token é Guid?
                if (frete.Token.HasValue)
                {
                    var cliente = await _context.Clientes.AsNoTracking()
                                       .FirstOrDefaultAsync(c => c.Token == frete.Token.Value);
                    if (!string.IsNullOrWhiteSpace(cliente?.CEP))
                        frete.Cep = cliente!.CEP;
                }

                using var connection = new SqlConnection(_context.Database.GetConnectionString());
                await connection.OpenAsync();

                using var command = new SqlCommand("Frete_sp_ObterValorPrazoPorCep", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@CepUsuario", frete.Cep);

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new RetornoDTO
                    {
                        Sucesso = true,
                        Objeto = new List<FreteResultadoDto> {
                            new FreteResultadoDto {
                                Transportadora = "Desconhecida",
                                Valor = 0, PrazoEntrega = 0,
                                Mensagem = "Infelizmente ainda não realizamos entregas para esse CEP."
                            }
                        },
                        Mensagem = "Infelizmente ainda não realizamos entregas para esse CEP.",
                        Status = StatusRetorno.OK
                    };
                }

                decimal valorBase = reader.GetDecimal(reader.GetOrdinal("Valor"));
                int prazo = reader.GetInt32(reader.GetOrdinal("Prazo"));

                int totalQuantidade = Math.Max(0, frete.Produtos?.Sum(p => p.Quantidade) ?? 0);
                if (totalQuantidade > 15)
                {
                    const string msg = "Nos desculpe, mas atualmente enviamos apenas 15 peças por pedido. Por favor, ajuste a quantidade.";
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Objeto = new List<FreteResultadoDto> {
                            new FreteResultadoDto { Transportadora = "Desconhecida", Valor = 0, PrazoEntrega = 0, Mensagem = msg }
                        },
                        Mensagem = msg,
                        Status = StatusRetorno.BadRequest
                    };
                }

                // Frete grátis por cupom?
                if (!string.IsNullOrWhiteSpace(frete.Cupom))
                {
                    var cupomFrete = await ValidarCupom(frete.Cupom);

                    if (cupomFrete is { Sucesso: true, Objeto: CupomResultadoDto { FreteGratis: true } })
                    {
                        return new RetornoDTO
                        {
                            Sucesso = true,
                            Objeto = new List<FreteResultadoDto>
                            {
                                new FreteResultadoDto { Transportadora = "Entrega Fixa", Valor = 0, PrazoEntrega = prazo }
                            },
                            Mensagem = "Frete calculado com sucesso.",
                            Status = StatusRetorno.OK
                        };
                    }
                }

                // adicional por quantidade: a cada 3 peças, +3,00
                int trios = totalQuantidade / 3;
                decimal adicional = trios * 3m;
                valorBase += adicional;

                return new RetornoDTO
                {
                    Sucesso = true,
                    Objeto = new List<FreteResultadoDto> {
                        new FreteResultadoDto { Transportadora = "Entrega Fixa", Valor = valorBase, PrazoEntrega = prazo }
                    },
                    Mensagem = "Frete calculado com sucesso.",
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular frete");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Objeto = new List<FreteResultadoDto> {
                        new FreteResultadoDto { Transportadora = "Erro", Valor = -1, PrazoEntrega = 0, Mensagem = "Erro interno ao calcular o frete." }
                    },
                    Mensagem = "Erro interno ao calcular o frete.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }
        #endregion

        #region Mercado Pago Helpers (apenas appsettings)
        // LÊ SOMENTE DE appsettings.{Environment}.json
        private string GetMpAccessToken()
        {
            var token = _config["MercadoPago:AccessToken"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("MercadoPago:AccessToken não configurado no appsettings.");
            return token;
        }

        private async Task<(bool Aprovado, string? Status, string? Raw)> VerificarPagamentoAprovadoAsync(string gatewayPaymentId)
        {
            var token = GetMpAccessToken();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{gatewayPaymentId}");
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return (false, null, raw);

            var json = JObject.Parse(raw);
            var status = json["status"]?.ToString();
            return (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase), status, raw);
        }

        // Endpoint de cartão (produção)
        public async Task<RetornoDTO> PagarCartaoMercadoPago(CardPayDto dto, string? tokenStr)
        {
            try
            {
                if (dto == null || dto.Produtos == null || dto.Produtos.Count == 0)
                    return new RetornoDTO { Sucesso = false, Mensagem = "Dados inválidos.", Status = StatusRetorno.BadRequest };

                // 1) Recalcula o valor total no servidor (NUNCA use valor do front)
                var calc = await CalcularValorTotal(new DadosCalculoValorDto
                {
                    Produtos = dto.Produtos.Select(p => new DadosProdutoDto
                    {
                        IdProduto = p.IdProduto,
                        Quantidade = p.Quantidade,
                        Tamanho = p.Tamanho
                    }).ToList(),
                    Cupom = dto.Cupom,
                    Cep = dto.Cep
                }, tokenStr);

                if (!calc.Sucesso || calc.Objeto is not ValorPagamentoDto valorDto || (valorDto.valorTotal ?? 0) <= 0)
                    return new RetornoDTO { Sucesso = false, Mensagem = "Falha ao calcular o valor.", Status = StatusRetorno.BadRequest };

                var amount = valorDto.valorTotal!.Value;

                // 2) Cria o pagamento no Mercado Pago
                var mpToken = GetMpAccessToken();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mpToken);
                client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

                var body = new
                {
                    transaction_amount = amount,
                    token = dto.Card.Token,
                    installments = dto.Card.Installments,
                    payment_method_id = dto.Card.PaymentMethodId,
                    issuer_id = dto.Card.IssuerId,
                    description = "Pagamento via cartão",
                    payer = new
                    {
                        email = dto.Card.Payer.Email,
                        identification = new
                        {
                            type = dto.Card.Payer.IdentificationType,    // "CPF"
                            number = dto.Card.Payer.IdentificationNumber // "12345678901"
                        }
                    }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                var resp = await client.PostAsync(
                    "https://api.mercadopago.com/v1/payments",
                    new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                );

                var raw = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro MP cartao: {Status} - {Body}", resp.StatusCode, raw);
                    return new RetornoDTO { Sucesso = false, Mensagem = "Falha ao criar pagamento com o gateway.", Status = StatusRetorno.BadRequest };
                }

                var jo = JObject.Parse(raw);
                var status = jo["status"]?.ToString();                 // "approved", "in_process", "rejected"...
                var gatewayPaymentId = jo["id"]?.ToString();

                if (string.IsNullOrWhiteSpace(gatewayPaymentId))
                    return new RetornoDTO { Sucesso = false, Mensagem = "Gateway não retornou ID do pagamento.", Status = StatusRetorno.InternalServerError };

                if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    var detail = jo["status_detail"]?.ToString();
                    var msg = $"Pagamento não aprovado (status: {status}{(string.IsNullOrEmpty(detail) ? "" : $" - {detail}")}).";
                    return new RetornoDTO { Sucesso = false, Mensagem = msg, Status = StatusRetorno.BadRequest };
                }

                // 3) Aprovado → registra o pedido (SP)
                int? clienteId = null;
                if (!string.IsNullOrWhiteSpace(tokenStr) && Guid.TryParse(tokenStr, out var tokenGuid))
                {
                    var pToken = new SqlParameter("@Token", SqlDbType.UniqueIdentifier) { Value = tokenGuid };
                    var cliente = await _context.Clientes
                        .FromSqlRaw("EXEC ObterClientePorToken @Token", pToken)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (cliente != null) clienteId = cliente.ClienteID;
                }

                // normaliza CEP (fallback de DadosEnvio)
                string cep = dto.Cep ?? "";
                if (string.IsNullOrWhiteSpace(cep) && !string.IsNullOrWhiteSpace(dto.DadosEnvio))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(dto.DadosEnvio);
                        if (doc.RootElement.TryGetProperty("cep", out var cepEl) && cepEl.ValueKind == JsonValueKind.String)
                            cep = cepEl.GetString() ?? "";
                        else if (doc.RootElement.TryGetProperty("endereco", out var endEl)
                            && endEl.ValueKind == JsonValueKind.Object
                            && endEl.TryGetProperty("cep", out var cepEl2)
                            && cepEl2.ValueKind == JsonValueKind.String)
                            cep = cepEl2.GetString() ?? "";
                    }
                    catch { /* ignora */ }
                }
                cep = System.Text.RegularExpressions.Regex.Replace(cep ?? "", @"\D", "");

                var paramProdutos = new SqlParameter("@Produtos", Newtonsoft.Json.JsonConvert.SerializeObject(dto.Produtos));
                var paramMetodo = new SqlParameter("@MetodoPagamento", 2); // 2 = cartão
                var paramEnvio = new SqlParameter("@DadosEnvio", (object)(dto.DadosEnvio ?? "") ?? DBNull.Value);
                var paramCupom = new SqlParameter("@Cupom", (object)(dto.Cupom ?? string.Empty).ToString().Trim() ?? DBNull.Value);
                var paramCliente = new SqlParameter("@ClienteID", (object?)clienteId ?? DBNull.Value);
                var paramFrete = new SqlParameter("@ValorFrete", SqlDbType.Decimal) { IsNullable = true, Precision = 18, Scale = 2, Value = DBNull.Value };
                var paramCep = new SqlParameter("@Cep", SqlDbType.VarChar, 20) { Value = (object)(cep ?? "") ?? DBNull.Value };

                // salva gatewayPaymentId em DadosPagamento
                var dadosPgJson = System.Text.Json.JsonSerializer.Serialize(new { gatewayPaymentId, status });
                var paramDadosPg = new SqlParameter("@DadosPagamento", (object)dadosPgJson ?? DBNull.Value);

                var outOk = new SqlParameter("@Sucesso", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var outMsg = new SqlParameter("@Mensagem", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
                var outId = new SqlParameter("@PedidoID", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC RegistrarPagamento @Produtos, @MetodoPagamento, @DadosEnvio, @Cupom, @ClienteID, @ValorFrete, @Cep, @DadosPagamento, @Sucesso OUTPUT, @Mensagem OUTPUT, @PedidoID OUTPUT",
                    paramProdutos, paramMetodo, paramEnvio, paramCupom, paramCliente, paramFrete, paramCep, paramDadosPg,
                    outOk, outMsg, outId
                );

                bool sucesso = Convert.ToBoolean(outOk.Value);
                string mensagem = outMsg.Value?.ToString() ?? "Sem mensagem.";
                int? pedidoId = outId.Value != DBNull.Value ? Convert.ToInt32(outId.Value) : null;

                if (!sucesso)
                    return new RetornoDTO { Sucesso = false, Mensagem = mensagem, Status = StatusRetorno.BadRequest };

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = mensagem,
                    Objeto = new { pedidoId = clienteId == null ? pedidoId : (int?)null }, // guest recebe id
                    Status = StatusRetorno.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pagar com cartão (Mercado Pago).");
                return new RetornoDTO
                {
                    Sucesso = false,
                    Mensagem = "Erro interno ao processar pagamento com cartão.",
                    Status = StatusRetorno.InternalServerError
                };
            }
        }
        #endregion
    }
}
