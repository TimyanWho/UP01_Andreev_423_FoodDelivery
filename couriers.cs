

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class couriers
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public couriers()
        {
            this.deliveries = new HashSet<deliveries>();
        }
    
        public int id { get; set; }
        public int user_id { get; set; }
        public string vehicle_type { get; set; }
        public bool is_active { get; set; }
    
        public virtual users users { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<deliveries> deliveries { get; set; }
    }
}
