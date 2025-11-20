using System;

namespace BCSH2_RPG_Generator.Data
{
    public class Hra
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nazev { get; set; }
        public string? Popis { get; set; }

        public Hra() { }
        public Hra(string nazev, string? popis = null, string? cesta = null)
        {
            Nazev = nazev;
            Popis = popis;
        }

        public override bool Equals(object? obj)
        {
            return obj is Hra hra && hra.Nazev == Nazev;
        }

        public override int GetHashCode()
        {
            return Nazev.GetHashCode();
        }
    }
}
