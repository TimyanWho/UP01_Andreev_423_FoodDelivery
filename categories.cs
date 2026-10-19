

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class categories
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public categories()
        {
            this.dishes = new HashSet<dishes>();
        }
    
        public int id { get; set; }
        public int restaurant_id { get; set; }
        public string name { get; set; }
    
        public virtual restaurants restaurants { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<dishes> dishes { get; set; }
    }
}
