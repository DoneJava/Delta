using DELTAAPI.Controllers;
using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.Enums;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    public class PagamentoService
    {
        #region Fields
        private readonly DeltaContext _context;
        private readonly ILogger<PagamentoService> _logger;
        #endregion

        #region Construtores
        public PagamentoService(ILogger<PagamentoService> logger, DeltaContext context)
        {
            _context = context;
            _logger = logger;
        }
        #endregion

        #region Métodos Públicos
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

                Pagamento? pagamento = await Task.Run(() =>
                    _context.Pagamentos
                        .FromSqlRaw("EXEC ObterPagamentoPorID @PagamentoID", param)
                        .AsNoTracking()
                        .AsEnumerable()
                        .FirstOrDefault());

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

                var resultado = new CupomResultadoDto
                {
                    Valido = (bool)(paramValido.Value ?? false),
                    DescontoPorcentagem = paramPorcentagem.Value != DBNull.Value ? (int)paramPorcentagem.Value : 0,
                    DescontoValor = paramValor.Value != DBNull.Value ? (decimal)paramValor.Value : 0
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
                    var retornoCupom = await ValidarCupom(dto.Cupom); // Chama o ÚNICO método
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

                // Frete
                string? cep = dto.Cep;

                if (string.IsNullOrWhiteSpace(cep) && !string.IsNullOrWhiteSpace(tokenStr))
                {
                    Cliente? usuario = await Task.Run(() =>
                        _context.Clientes
                            .FromSqlRaw("EXEC ObterClientePorToken @Token", new SqlParameter("@Token", tokenStr))
                            .AsEnumerable()
                            .SingleOrDefault()
                    );

                    if (usuario != null && !string.IsNullOrWhiteSpace(usuario.CEP))
                        cep = usuario.CEP;
                }

                if (!string.IsNullOrWhiteSpace(cep))
                {
                    // Simula frete: ajuste se usar Service real.
                    decimal valorFrete = 20; // Exemplo fixo
                    valorFinal += valorFrete;
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

        public async Task<RetornoDTO> ProcessarPagamento(PagamentoProcessarDto dto, string? tokenStr)
        {
            try
            {
                int? clienteId = null;

                if (!string.IsNullOrWhiteSpace(tokenStr) && tokenStr != "null")
                {
                    Cliente? cliente = await _context.Clientes
                        .FromSqlRaw("EXEC ObterClientePorToken @Token", new SqlParameter("@Token", tokenStr))
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (cliente != null)
                        clienteId = cliente.ClienteID;
                }

                if (dto.Produtos == null || dto.Produtos.Count == 0)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Nenhum produto informado.",
                        Status = StatusRetorno.BadRequest
                    };
                }

                // 🔢 Calcular frete
                decimal valorFrete = 0;
                try
                {
                    // Aqui você pode chamar um FreteService real se quiser.
                    valorFrete = 20; // Simulação
                }
                catch
                {
                    valorFrete = 0; // Continua com zero se erro
                }

                // 🛠 Chamada da procedure
                SqlParameter paramProdutos = new SqlParameter("@Produtos", Newtonsoft.Json.JsonConvert.SerializeObject(dto.Produtos));
                SqlParameter paramMetodo = new SqlParameter("@MetodoPagamento", dto.MetodoPagamento);
                SqlParameter paramEnvio = new SqlParameter("@DadosEnvio", dto.DadosEnvio ?? "");
                SqlParameter paramCupom = new SqlParameter("@Cupom", dto.Cupom ?? "");
                SqlParameter paramValorFrete = new SqlParameter("@ValorFrete", valorFrete);
                SqlParameter paramClienteId = new SqlParameter("@ClienteID", clienteId ?? (object)DBNull.Value);
                SqlParameter paramSucesso = new SqlParameter("@Sucesso", System.Data.SqlDbType.Bit) { Direction = System.Data.ParameterDirection.Output };
                SqlParameter paramMensagem = new SqlParameter("@Mensagem", System.Data.SqlDbType.NVarChar, 500) { Direction = System.Data.ParameterDirection.Output };
                SqlParameter paramPedidoId = new SqlParameter("@PedidoID", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC RegistrarPagamento @Produtos, @MetodoPagamento, @DadosEnvio, @Cupom, @ClienteID, @ValorFrete, @Sucesso OUTPUT, @Mensagem OUTPUT, @PedidoID OUTPUT",
                    paramProdutos, paramMetodo, paramEnvio, paramCupom,
                    paramClienteId, paramValorFrete, paramSucesso, paramMensagem, paramPedidoId
                );

                bool sucesso = Convert.ToBoolean(paramSucesso.Value);
                string mensagem = paramMensagem.Value?.ToString() ?? "Sem mensagem de retorno.";
                int? pedidoId = paramPedidoId.Value != DBNull.Value ? Convert.ToInt32(paramPedidoId.Value) : null;

                if (!sucesso)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = mensagem,
                        Status = StatusRetorno.BadRequest
                    };
                }

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = mensagem,
                    Objeto = new
                    {
                        pedidoId = clienteId == null ? pedidoId : null
                    },
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

                // Chama seu cálculo padrão
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

                // ⚡ Chamada API externa MercadoPago (exemplo fixo)
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    "TEST-4996573331239532-050700-8c6c9c48bc61726511f212cdd9df171f-570054060"
                );
                client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

                string? email = "comprador@teste.com";

                string bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    transaction_amount = valor,
                    description = "Pagamento via Pix (sandbox)",
                    payment_method_id = "pix",
                    payer = new { email = email }
                });

                HttpResponseMessage response = await client.PostAsync(
                    "https://api.mercadopago.com/v1/payments",
                    new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json")
                );

                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao gerar pagamento Pix.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                var dados = Newtonsoft.Json.Linq.JObject.Parse(responseJson);

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
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    "TEST-4996573331239532-050700-8c6c9c48bc6172651ff212cdd9df171f-570054060"
                );

                HttpResponseMessage response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{id}");
                string respostaJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao consultar status do pagamento PIX. Status: {Status}, Resposta: {Resposta}", response.StatusCode, respostaJson);

                    return new RetornoDTO
                    {
                        Sucesso = false,
                        Mensagem = "Erro ao consultar status do pagamento Pix.",
                        Status = StatusRetorno.InternalServerError
                    };
                }

                var dados = Newtonsoft.Json.Linq.JObject.Parse(respostaJson);

                string? status = dados["status"]?.ToString();
                string? statusDetail = dados["status_detail"]?.ToString();

                return new RetornoDTO
                {
                    Sucesso = true,
                    Mensagem = "Status do pagamento PIX consultado com sucesso.",
                    Objeto = new
                    {
                        status = status,
                        detalhe = statusDetail
                    },
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
    }
}
