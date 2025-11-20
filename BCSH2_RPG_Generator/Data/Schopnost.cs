using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCSH2_RPG_Generator.Data
{
    public class Schopnost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nazev { get; set; }
        public string Typ { get; set; }
        public string Popis { get; set; }
        public string? Obrazek { get; set; }

        public Schopnost() { }

        public Schopnost(string nazev, string typ, string popis, string? obrazek = null)
        {
            Nazev = nazev;
            Typ = typ;
            Popis = popis;
            Obrazek = obrazek;
        }

        public override bool Equals(object? obj)
        {
            return obj is Schopnost schopnost && schopnost.Nazev == Nazev;
        }

        public override int GetHashCode()
        {
            return Nazev.GetHashCode();
        }

    }

}
