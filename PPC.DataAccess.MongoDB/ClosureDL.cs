﻿using System.Collections.Generic;
using MongoDB.Driver;
using PPC.Domain;
using PPC.IDataAccess;

namespace PPC.DataAccess.MongoDB
{
    public class ClosureDL : IClosureDL
    {
        private const string DatabaseName = "PPCClub";
        private const string CollectionName = "Closures";

        private IMongoCollection<Closure> ClosureCollection => _db.GetCollection<Closure>(CollectionName);

        private readonly IMongoDatabase _db;

        public ClosureDL()
        {
            var client = new MongoClient();
            _db = client.GetDatabase(DatabaseName);
        }

        public void Fetch(IEnumerable<Closure> closures)
        {
            _db.DropCollection(CollectionName);
            ClosureCollection.InsertMany(closures);
        }

        #region IClosureDL

        public void SaveClosure(Closure closure)
        {
            ClosureCollection.InsertOne(closure);
        }

        #endregion
    }
}
