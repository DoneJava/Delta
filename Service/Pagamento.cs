using DELTAAPI.Data;
using DELTAAPI.Enums;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Service
{
    public class Pagamento
    {
        private readonly DeltaContext _context;
        public Pagamento(DeltaContext context)
        {
            _context = context;
        }
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

    }
}
