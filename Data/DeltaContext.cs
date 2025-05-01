using DELTAAPI.DTOs;
using DELTAAPI.Models;
using Microsoft.EntityFrameworkCore;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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


            modelBuilder.Entity<ProdutoDto>().HasNoKey(); // ← necessário

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
