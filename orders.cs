

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class orders
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public orders()
        {
            this.deliveries = new HashSet<deliveries>();
            this.order_items = new HashSet<order_items>();
            this.payments = new HashSet<payments>();
            this.reviews = new HashSet<reviews>();
        }
    
        public int id { get; set; }
        public int user_id { get; set; }
        public int restaurant_id { get; set; }
        public int address_id { get; set; }
        public string status { get; set; }
        public string payment_method { get; set; }
        public decimal total_amount { get; set; }
        public System.DateTime created_at { get; set; }
        public string comment { get; set; }
    
        public virtual addresses addresses { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<deliveries> deliveries { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<order_items> order_items { get; set; }
        public virtual restaurants restaurants { get; set; }
        public virtual users users { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<payments> payments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<reviews> reviews { get; set; }
    }
}
