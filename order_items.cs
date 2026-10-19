

namespace UP01_Andreev_423_FoodDelivery
{
    using System;
    using System.Collections.Generic;
    
    public partial class order_items
    {
        public int id { get; set; }
        public int order_id { get; set; }
        public int dish_id { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
    
        public virtual dishes dishes { get; set; }
        public virtual orders orders { get; set; }
    }
}
