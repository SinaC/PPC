﻿using System;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace PPC.Domain.v2
{
    [DataContract(Namespace = "")]
    public class Article
    {
        [BsonId]
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Ean { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string SubCategory { get; set; }

        [DataMember]
        public string Producer { get; set; }

        [DataMember]
        public decimal SupplierPrice { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public int Stock { get; set; }

        [DataMember]
        public decimal VatRate { get; set; }

        [DataMember]
        public Guid OriginalId { get; set; }
    }
}
