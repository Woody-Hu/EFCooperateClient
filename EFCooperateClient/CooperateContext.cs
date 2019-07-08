using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EFCooperateClient
{
    public class CooperateContext:DbContext
    {
        public CooperateContext(DbContextOptions<CooperateContext> options):base(options)
        { }

        internal DbSet<CooperateEntity> CooperateEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new CooperateEntityConfiguration());
        }
    }
}
