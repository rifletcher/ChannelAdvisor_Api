using System;
using System.Collections.Generic;

namespace ChannelAdvisor_Api.Models
{
    public class ChannelAdvisorSessionResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class ChannelAdvisorOrder
    {
        public string OrderId { get; set; }
        public DateTime InsertDate { get; set; }
        public string Phone { get; set; }
        public string ContactName { get; set; }
        public string Company { get; set; }
        public string Street { get; set; }
        public string District { get; set; }
        public string CityTown { get; set; }
        public string County { get; set; }
        public string PostCode { get; set; }
        public string CountryCode { get; set; }
        public string ShipReference { get; set; }
        public decimal Weight { get; set; }
        public decimal Value { get; set; }
        public string Content { get; set; }
        public string Instructions { get; set; }
        public string Email { get; set; }
        public List<ChannelAdvisorLineItem> LineItems { get; set; }
    }

    public class ChannelAdvisorLineItem
    {
        public decimal Weight { get; set; }
        public string Content { get; set; }
        public decimal Value { get; set; }
        public string ShipReference { get; set; }
        public string WarehouseLocation { get; set; }

    }

    public class ChannelAdvisorAccount
    {
        public string AccountId { get; set; }
        public int LocalId { get; set; }
        public string Status { get; set; }
    }

}