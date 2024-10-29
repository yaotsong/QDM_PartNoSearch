using System;
using Microsoft.EntityFrameworkCore;

namespace QDM_PartNoSearch.Models
{
    public partial class DeanContext : DbContext
    {
        public DeanContext()
        {
        }

        public DeanContext(DbContextOptions<DeanContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PredictPartNo> PredictPartNo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("Chinese_Taiwan_Stroke_BIN");

            modelBuilder.Entity<PredictPartNo>(entity =>
            {
                entity.HasKey(e => new { e.DateTime, e.PartNo });

                entity.ToTable("PredictPartNo");

                entity.Property(e => e.DateTime)
                    .HasColumnType("datetime") // 根據需要設定為 datetime
                    .HasDefaultValue(null) // 使用 SQL Server 的當前時間
                    .HasColumnName("DateTime");

                entity.Property(e => e.PartNo)
                    .HasMaxLength(20)
                    .HasDefaultValue(null) // 預設值為 null
                    .HasColumnName("PartNo");

                entity.Property(e => e.Num)
                    .HasMaxLength(10)
                    .HasDefaultValue(null) // 預設值為 null
                    .HasColumnName("NUM");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
