using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCooperateClient
{
    class CooperateEntityConfiguration : IEntityTypeConfiguration<CooperateEntity>
    {
        public void Configure(EntityTypeBuilder<CooperateEntity> builder)
        {
            builder.HasKey(k => k.Id);
            builder.Property(k => k.LastModifyDateTime).IsConcurrencyToken();
        }
    }
}
