﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WisePay.Web.Purchases.Models
{
    public class PurchaseForMe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; }

        public decimal Amount { get; set; }
        public bool IsPayedOff { get; set; }
    }
}
