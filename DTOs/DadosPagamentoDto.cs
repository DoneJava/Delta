namespace DELTAAPI.DTOs
{
    public class PagamenDadosPagamentoDtotoDTO
    {
        // Dados do Produto
        public List<ProdutoDTO>? Produtos { get; set; }

        // Método de Pagamento Selecionado
        public int MetodoPagamento { get; set; }

        // Dados do Pagamento
        public DadosPagamentoDTO? DadosPagamento { get; set; }

        // Dados para Envio
        public DadosEnvioDTO? DadosEnvio { get; set; }

        // Cupom de Desconto
        public string? Cupom { get; set; }
    }

    public class ProdutoDTO
    {
        public int IdProduto { get; set; }
        public string? Tamanho { get; set; }
        public int Quantidade { get; set; }
    }

    public class DadosPagamentoDTO
    {
        public string? Tipo { get; set; } // Ex: "PIX", "Cartão de Crédito", "Boleto", etc.
        public string? ChavePix { get; set; } // Chave PIX, se for o método de pagamento
        public string? Numero { get; set; } // Número do Cartão (se método de pagamento for Cartão de Crédito/Débito)
        public string? Nome { get; set; } // Nome no Cartão
        public string? Validade { get; set; } // Validade do Cartão (MM/AA)
        public string? CVV { get; set; } // CVV do Cartão
        public int Parcelas { get; set; } // Parcelas do Cartão, caso se aplique
        public string? DetalhesBoleto { get; set; } // Detalhes para Boleto, caso seja o método de pagamento
    }

    public class DadosEnvioDTO
    {
        public string? Nome { get; set; } // Nome Completo do Usuário
        public string? Endereco { get; set; } // Endereço para Envio
        public string? Complemento { get; set; } // Complemento (opcional)
        public string? Cep { get; set; } // CEP para Envio
        public string? Cpf { get; set; } // CPF
        public bool Portaria24h { get; set; } // Indica se o local possui portaria 24h
        public bool UsarDadosUsuarioLogado { get; set; } // Se o usuário escolheu usar os dados do usuário logado
    }
}
