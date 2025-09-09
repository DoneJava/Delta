using DELTAAPI.DTOs;
using DELTAAPI.Model;
using DELTAAPI.Models;
using Microsoft.EntityFrameworkCore;
using static DELTAAPI.Service.PagamentoService;

namespace DELTAAPI.Data
{
    public class DeltaContext : DbContext
    {
        public DeltaContext(DbContextOptions<DeltaContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<ImagemProduto> ImagemProdutos { get; set; }
        public DbSet<ProdutoDto> ProdutoDtoRaw { get; set; }
        public DbSet<Contato> Contatos { get; set; }
        public DbSet<Cupom> Cupons { get; set; }
        public DbSet<TokenMelhorEnvio> TokenMelhorEnvio { get; set; }
        public DbSet<PedidoDto> PedidtosDto { get; set; }
        public DbSet<ItemDto> ItemDto { get; set; }
        public DbSet<FretePorEstado> FretePorEstado { get; set; }
        public DbSet<Associado> Associados { get; set; }
        public DbSet<Versiculos> Versiculos { get; set; }
        public DbSet<BibliaLivroAlias> BibliaLivroAlias { get; set; }
        public DbSet<Visita> Visitas { get; set; }
        public DbSet<PageView> PageViews { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClienteIdDto>().HasNoKey().ToView(null);
            modelBuilder.Entity<PedidoCompletoDto>().HasNoKey().ToView(null);
            modelBuilder.Entity<ItemDto>().HasNoKey().ToView(null);
            modelBuilder.Entity<FreteValorTmpDTO>().HasNoKey();

            modelBuilder.Entity<Associado>(e =>
            {
                e.ToTable("Associado");
                e.HasKey(x => x.AssociadoId);
                e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
                e.Property(x => x.Documento).HasMaxLength(20);
                e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
                e.Property(x => x.Ativo).IsRequired();
                e.Property(x => x.CriadoEmUtc).HasColumnType("datetime2(3)");
            });


            // Mapeamento de Cliente
            modelBuilder.Entity<Cliente>()
                .HasKey(c => c.ClienteID); // Chave primária

            // Mapeamento de Produto
            modelBuilder.Entity<Produto>()
                .HasKey(p => p.ProdutoID); // Chave primária


            // Mapeamento de Pedido
            modelBuilder.Entity<Pedido>()
                .HasKey(p => p.PedidoID); // Chave primária

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteID)
                .OnDelete(DeleteBehavior.Cascade); // Relacionamento com Cliente

            // Mapeamento de ItemPedido
            modelBuilder.Entity<ItemPedido>()
                .HasKey(ip => ip.ItemPedidoID); // Chave primária

            modelBuilder.Entity<ItemPedido>()
                .HasOne(ip => ip.Pedido)
                .WithMany()
                .HasForeignKey(ip => ip.PedidoID)
                .OnDelete(DeleteBehavior.Cascade); // Relacionamento com Pedido

            modelBuilder.Entity<ItemPedido>()
                .HasOne(ip => ip.Produto)
                .WithMany()
                .HasForeignKey(ip => ip.ProdutoID)
                .OnDelete(DeleteBehavior.Restrict); // Relacionamento com Produto

            // Mapeamento de Pagamento
            modelBuilder.Entity<Pagamento>()
                .HasKey(p => p.PagamentoID); // Chave primária

            modelBuilder.Entity<Pagamento>()
                .HasOne(p => p.Pedido)
                .WithMany()
                .HasForeignKey(p => p.PedidoID)
                .OnDelete(DeleteBehavior.Cascade); // Relacionamento com Pedido

            // Mapeamento de Envio
            modelBuilder.Entity<Envio>()
                .HasKey(e => e.EnvioID); // Chave primária

            modelBuilder.Entity<Envio>()
                .HasOne(e => e.Pedido)
                .WithMany()
                .HasForeignKey(e => e.PedidoID)
                .OnDelete(DeleteBehavior.Cascade); // Relacionamento com Pedido

            // Mapeamento de ImagemProduto
            modelBuilder.Entity<ImagemProduto>()
                .HasKey(ip => ip.ImagemID); // Chave primária

            modelBuilder.Entity<ImagemProduto>()
                .HasOne(ip => ip.Produto)
                .WithMany()
                .HasForeignKey(ip => ip.ProdutoID)
                .OnDelete(DeleteBehavior.Cascade); // Relacionamento com Produto

            modelBuilder.Entity<Pagamento>()
                .Property(p => p.MetodoPagamento)
                .HasConversion<int>();

            modelBuilder.Entity<Pagamento>()
                .Property(p => p.StatusPagamento)
                .HasConversion<int>();

            modelBuilder.Entity<Contato>().ToTable("Contato");

            // Mapeamento de Versiculos
            modelBuilder.Entity<Versiculos>()
                .HasKey(c => c.VersiculoId); // Chave primária

            // Mapeamento de BibliaLivroAlias
            modelBuilder.Entity<BibliaLivroAlias>()
                .HasKey(c => c.Id); // Chave primária


            modelBuilder.Entity<ProdutoDto>().HasNoKey(); // ← necessário
            modelBuilder.Entity<ItemDto>().HasNoKey(); // ← necessário

            modelBuilder.Entity<Cupom>()
                .Property(c => c.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Cupom>()
                .Property(c => c.DescontoPorcentagem)
                .IsRequired(false);

            modelBuilder.Entity<Cupom>()
                .Property(c => c.DescontoValor)
                .HasPrecision(18, 2)
                .IsRequired(false);

            modelBuilder.Entity<Cupom>()
                .Property(c => c.FreteGratis)
                .IsRequired(false);
            
            modelBuilder.Entity<FretePorEstado>(entity =>
            {
                entity.ToTable("FretePorEstado");

                entity.HasKey(f => f.Id);

                entity.Property(f => f.UF)
                    .IsRequired()
                    .HasMaxLength(2)
                    .IsFixedLength(true);

                entity.Property(f => f.Valor)
                    .HasPrecision(10, 2)
                    .IsRequired();

                entity.Property(f => f.Prazo)
                    .IsRequired();

                entity.Property(f => f.CepInicial)
                    .HasMaxLength(8)
                    .IsFixedLength(true)
                    .IsRequired();

                entity.Property(f => f.CepFinal)
                    .HasMaxLength(8)
                    .IsFixedLength(true)
                    .IsRequired();
            });

            modelBuilder.Entity<Visita>(e =>
            {
                e.ToTable("Visita");
                e.HasKey(v => v.VisitaId);

                e.Property(v => v.VisitaId)
                    .ValueGeneratedOnAdd();

                e.Property(v => v.Dia)
                    .HasColumnType("date")
                    .IsRequired();

                e.Property(v => v.AnonId)
                    .HasColumnType("uniqueidentifier");

                e.Property(v => v.Url)
                    .HasMaxLength(500);

                e.Property(v => v.Referrer)
                    .HasMaxLength(500);

                e.Property(v => v.UtmSource)
                    .HasMaxLength(100);

                e.Property(v => v.UtmMedium)
                    .HasMaxLength(100);

                e.Property(v => v.UtmCampaign)
                    .HasMaxLength(100);

                e.Property(v => v.Ip)
                    .HasMaxLength(45)           // IPv6 cabe
                    .IsUnicode(false);

                e.Property(v => v.UserAgent)
                    .HasMaxLength(400);

                e.Property(v => v.CreatedAtUtc)
                    .HasColumnType("datetime2(0)")
                    .HasDefaultValueSql("SYSUTCDATETIME()")
                    .IsRequired();

                // Índice único por (Dia, AnonId) quando AnonId não é nulo
                e.HasIndex(v => new { v.Dia, v.AnonId })
                    .IsUnique()
                    .HasFilter("[AnonId] IS NOT NULL")
                    .HasDatabaseName("UX_Visita_Dia_AnonId");
            });

            modelBuilder.Entity<Visita>(e =>
            {
                e.ToTable("Visita");
                e.HasKey(x => x.VisitaId);
                e.Property(x => x.Url).HasMaxLength(800);
                e.Property(x => x.Referrer).HasMaxLength(800);
                e.Property(x => x.UtmSource).HasMaxLength(100);
                e.Property(x => x.UtmMedium).HasMaxLength(100);
                e.Property(x => x.UtmCampaign).HasMaxLength(100);
                e.Property(x => x.UserAgent).HasMaxLength(400);
                e.Property(x => x.Ip).HasMaxLength(45);
            });

            modelBuilder.Entity<PageView>(e =>
            {
                e.ToTable("PageView");
                e.HasKey(x => x.PageViewId);
                e.Property(x => x.CriadoEmBrt)
                 .HasColumnType("datetime2(0)")
                 .HasDefaultValueSql("CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'E. South America Standard Time' AS DATETIME2(0))");
                e.Property(x => x.Route).HasMaxLength(200).IsRequired();
                e.Property(x => x.Url).HasMaxLength(800);
                e.Property(x => x.UserAgent).HasMaxLength(400);
                e.Property(x => x.Ip).HasMaxLength(45);
            });


            modelBuilder.Entity<TokenMelhorEnvio>(entity =>
            {
                entity.ToTable("TokenMelhorEnvio");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.AccessToken).IsRequired().HasMaxLength(1000);
                entity.Property(t => t.RefreshToken).IsRequired().HasMaxLength(1000);
                entity.Property(t => t.Expiracao).IsRequired();
                entity.Property(t => t.CriadoEm).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(t => t.AtualizadoEm).HasDefaultValueSql("GETUTCDATE()");
            });


            // Mapear explicitamente o nome das tabelas (caso seja necessário)
            modelBuilder.Entity<Cliente>().ToTable("Cliente");
            modelBuilder.Entity<Produto>().ToTable("Produto");
            modelBuilder.Entity<Pedido>().ToTable("Pedido");
            modelBuilder.Entity<ItemPedido>().ToTable("ItemPedido");
            modelBuilder.Entity<Pagamento>().ToTable("Pagamento");
            modelBuilder.Entity<Envio>().ToTable("Envio");
            modelBuilder.Entity<ImagemProduto>().ToTable("ImagemProduto");

            base.OnModelCreating(modelBuilder);
        }
    }
}
