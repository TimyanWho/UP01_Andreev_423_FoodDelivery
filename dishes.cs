

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class dishes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public dishes()
        {
            this.order_items = new HashSet<order_items>();
        }
    
        public int id { get; set; }
        public int category_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public decimal price { get; set; }
        public bool is_available { get; set; }
    
        public virtual categories categories { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<order_items> order_items { get; set; }
    }
}
