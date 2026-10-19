

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class reviews
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int restaurant_id { get; set; }
        public int order_id { get; set; }
        public int rating { get; set; }
        public string comment { get; set; }
        public System.DateTime created_at { get; set; }
    
        public virtual orders orders { get; set; }
        public virtual restaurants restaurants { get; set; }
        public virtual users users { get; set; }
    }
}
