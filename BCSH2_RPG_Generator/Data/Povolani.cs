using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCSH2_RPG_Generator.Data
{
    public class Povolani
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nazev { get; set; }
        public string Popis { get; set; }
        public string? Obrazek { get; set; }

        public Povolani() { }
        public Povolani(string nazev, string popis, string? obrazek = null)
        {
            Nazev = nazev;
            Popis = popis;
            Obrazek = obrazek;
        }

        public override bool Equals(object? obj)
        {
            return obj is Povolani povolani && povolani.Nazev == Nazev;
        }

        public override int GetHashCode()
        {
            return Nazev.GetHashCode();
        }

    }

}
