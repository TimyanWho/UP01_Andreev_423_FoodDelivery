

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class deliveries
    {
        public int id { get; set; }
        public int order_id { get; set; }
        public Nullable<int> courier_id { get; set; }
        public string status { get; set; }
        public Nullable<System.DateTime> assigned_at { get; set; }
        public Nullable<System.DateTime> picked_up_at { get; set; }
        public Nullable<System.DateTime> delivered_at { get; set; }
    
        public virtual couriers couriers { get; set; }
        public virtual orders orders { get; set; }
    }
}
