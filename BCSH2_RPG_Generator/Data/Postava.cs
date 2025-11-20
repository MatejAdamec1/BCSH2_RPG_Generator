using LiteDB;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BCSH2_RPG_Generator.Data
{
    public class Postava
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid HraId { get; set; }
        public string Jmeno { get; set; }
        public Guid RasaPostavy { get; set; }
        public Guid PovolaniPostavy { get; set; }
        public List<Guid> SchopnostiPostavy { get; set; } = new List<Guid>();
        public List<KeyValuePair<string, int>> Staty { get; set; } = new List<KeyValuePair<string, int>>();
        public string? Obrazek { get; set; }

        public Postava() { }

        public Postava(string jmeno, Guid rasaId, Guid povolaniId, List<Guid> schopnostiIds, List<KeyValuePair<string, int>> staty, Guid hraId, string? obrazek = null)
        {
            Jmeno = jmeno;
            RasaPostavy = rasaId;
            PovolaniPostavy = povolaniId;
            SchopnostiPostavy = schopnostiIds;
            Staty = staty;
            Obrazek = obrazek;
            HraId = hraId;
        }

        public override bool Equals(object? obj)
        {
            return obj is Postava postava && postava.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
