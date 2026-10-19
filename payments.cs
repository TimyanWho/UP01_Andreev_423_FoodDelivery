

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class payments
    {
        public int id { get; set; }
        public int order_id { get; set; }
        public decimal amount { get; set; }
        public string status { get; set; }
        public Nullable<System.DateTime> paid_at { get; set; }
    
        public virtual orders orders { get; set; }
    }
}
