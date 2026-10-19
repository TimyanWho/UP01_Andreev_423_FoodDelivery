

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class restaurants
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public restaurants()
        {
            this.categories = new HashSet<categories>();
            this.orders = new HashSet<orders>();
            this.reviews = new HashSet<reviews>();
        }
    
        public int id { get; set; }
        public string name { get; set; }
        public string address_text { get; set; }
        public string phone { get; set; }
        public string description { get; set; }
        public Nullable<decimal> rating { get; set; }
        public bool is_active { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<categories> categories { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<orders> orders { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<reviews> reviews { get; set; }
    }
}
