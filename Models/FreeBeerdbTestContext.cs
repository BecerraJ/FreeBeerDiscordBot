using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore;
using System;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DiscordBot.Models
{
  public partial class FreeBeerdbTestContext : DbContext
  {
    public FreeBeerdbTestContext()
    {
    }
    
    public FreeBeerdbTestContext(DbContextOptions<FreeBeerdbTestContext> options)
        : base(options)
    {
    }


    public virtual DbSet<MoneyType> MoneyType { get; set; }
    public virtual DbSet<Player> Player { get; set; }
    public virtual DbSet<PlayerLoot> PlayerLoot { get; set; }
    public virtual DbSet<LoggedPlayerInfo> LoggedPlayerInfo { get; set; }
    public virtual DbSet<RegisteredAllianceMembers> RegisteredAllianceMembers { get; set; }
    public virtual DbSet<RegisteredAllianceGuilds> RegisteredAllianceGuilds { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
        optionsBuilder.UseSqlServer("Server=.;Database=FreeBeerdb;Trusted_Connection=True; Encrypt=False;");

        optionsBuilder.UseMySql("YourMariaDBConnectionString", new MySqlServerVersion(new Version(10, 5, 12)));
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<MoneyType>(entity =>
      {
        entity.Property(e => e.Id).HasColumnName("id");
      });

      modelBuilder.Entity<Player>(entity =>
      {
        entity.Property(e => e.Id).HasColumnName("id");

        entity.Property(e => e.PlayerId)
                  .HasMaxLength(50)
                  .IsUnicode(false);

        entity.Property(e => e.PlayerName)
                  .HasMaxLength(50)
                  .IsUnicode(false);
      });

      modelBuilder.Entity<PlayerLoot>(entity =>
      {
        entity.Property(e => e.Id).HasColumnName("Id");

        entity.HasIndex(e => e.PlayerId);

        entity.Property(e => e.CreateDate)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("(GETUTCDATE())");

        entity.Property(e => e.KillId)
                  .IsRequired()
                  .HasMaxLength(250)
                  .IsUnicode(false);

        entity.Property(e => e.Loot).HasColumnType("decimal(18, 0)");

        entity.Property(e => e.Message)
                  .IsRequired()
                  .HasMaxLength(250)
                  .IsUnicode(false);

        entity.Property(e => e.PartyLeader)
                  .IsRequired()
                  .HasMaxLength(250)
                  .IsUnicode(false);

        entity.Property(e => e.PlayerId).HasColumnName("PlayerID");

        entity.Property(e => e.QueueId)
                  .IsRequired()
                  .HasMaxLength(250)
                  .IsUnicode(false);

        entity.Property(e => e.Reason)
                  .IsRequired()
                  .HasMaxLength(250)
                  .IsUnicode(false);

        entity.Property(e => e.TypeId).HasColumnName("TypeID");

        entity.HasOne(d => d.Player)
                  .WithMany(p => p.PlayerLoot)
                  .HasForeignKey(d => d.PlayerId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK__PlayerLoo__Playe__29572725");

        entity.HasOne(d => d.Type)
                  .WithMany(p => p.PlayerLoot)
                  .HasForeignKey(d => d.TypeId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK__PlayerLoo__TypeI__286302EC");
      });

      modelBuilder.Entity<RegisteredAllianceMembers>(entity =>
      {
        entity.Property(e => e.PlayerID).HasColumnName("PlayerID")
                    .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.HasIndex(e => e.PlayerID);

        entity.Property(e => e.PlayerName)
          .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.Property(e => e.GuildID).HasColumnName("GuildID");
        entity.Property(e => e.GuildID)
          .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.Property(e => e.GuildName)
          .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.Property(e => e.AllianceID).HasColumnName("AllianceID");
        entity.Property(e => e.AllianceID)
          .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.Property(e => e.AllianceName)
          .IsRequired()
          .HasMaxLength(50)
          .IsUnicode(false);

        entity.Property(e => e.DateRegistered)
          .HasColumnType("datetime")
          .HasDefaultValueSql("(GETUTCDATE())");

        entity.Property(e => e.KillFame).HasColumnName("KillFame");
      });

      OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
  }
}
