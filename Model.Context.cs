

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class UP01_Andreev_423_FoodDeliveryEntities : DbContext
    {
        public UP01_Andreev_423_FoodDeliveryEntities()
            : base("name=UP01_Andreev_423_FoodDeliveryEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<addresses> addresses { get; set; }
        public virtual DbSet<categories> categories { get; set; }
        public virtual DbSet<couriers> couriers { get; set; }
        public virtual DbSet<deliveries> deliveries { get; set; }
        public virtual DbSet<dishes> dishes { get; set; }
        public virtual DbSet<order_items> order_items { get; set; }
        public virtual DbSet<orders> orders { get; set; }
        public virtual DbSet<payments> payments { get; set; }
        public virtual DbSet<restaurants> restaurants { get; set; }
        public virtual DbSet<reviews> reviews { get; set; }
        public virtual DbSet<roles> roles { get; set; }
        public virtual DbSet<sysdiagrams> sysdiagrams { get; set; }
        public virtual DbSet<users> users { get; set; }
    }
}
