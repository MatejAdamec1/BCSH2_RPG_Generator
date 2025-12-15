using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BCSH2_RPG_Generator.Spravce
{
    public class SpravceDatLite<T> where T : class
    {
        private readonly string _dbPath;
        private readonly string _collectionName;

        public SpravceDatLite(string dbPath, string collectionName)
        {
            _dbPath = dbPath;
            _collectionName = collectionName;
        }

        public List<T> NactiVse()
        {
            using var db = new LiteDatabase(_dbPath);
            return db.GetCollection<T>(_collectionName).FindAll().ToList();
        }

        public void Pridej(T item)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<T>(_collectionName).Insert(item);
        }

        public void Aktualizuj(T item)
        {
            using var db = new LiteDatabase(_dbPath);
            db.GetCollection<T>(_collectionName).Update(item);
        }

        public void Smaz(Func<T, bool> predicate)
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<T>(_collectionName);
            var itemsToDelete = col.FindAll().Where(predicate).ToList();
            foreach (var item in itemsToDelete)
            {
                var idProp = typeof(T).GetProperty("Id");
                if (idProp != null)
                {
                    var idValue = idProp.GetValue(item);
                    if (idValue != null)
                    {
                        col.Delete(new BsonValue(idValue));
                    }
                }
            }
        }

    }
}
