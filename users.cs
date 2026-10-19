

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class users
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public users()
        {
            this.addresses = new HashSet<addresses>();
            this.couriers = new HashSet<couriers>();
            this.orders = new HashSet<orders>();
            this.reviews = new HashSet<reviews>();
        }
    
        public int id { get; set; }
        public int role_id { get; set; }
        public string login { get; set; }
        public string full_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string password_hash { get; set; }
        public System.DateTime created_at { get; set; }
        public bool is_active { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<addresses> addresses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<couriers> couriers { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<orders> orders { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<reviews> reviews { get; set; }
        public virtual roles roles { get; set; }
    }
}
