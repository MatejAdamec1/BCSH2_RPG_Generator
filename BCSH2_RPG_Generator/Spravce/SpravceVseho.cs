using BCSH2_RPG_Generator.Spravce;
using BCSH2_RPG_Generator.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BCSH2_RPG_Generator.Spravce
{
    public class SpravceVseho
    {
        public static SpravceVseho Instance { get; private set; }

        private readonly SpravceDatLite<Rasa> spravceRas;
        private readonly SpravceDatLite<Povolani> spravcePovolani;
        private readonly SpravceDatLite<Schopnost> spravceSchopnosti;
        private readonly SpravceDatLite<Postava> spravcePostav;
        private readonly SpravceDatLite<Hra> spravceHer;


        public SpravceVseho(string dbPath)
        {
            Instance = this;
            spravceRas = new SpravceDatLite<Rasa>(dbPath, "Rasy");
            spravcePovolani = new SpravceDatLite<Povolani>(dbPath, "Povolani");
            spravceSchopnosti = new SpravceDatLite<Schopnost>(dbPath, "Schopnosti");
            spravcePostav = new SpravceDatLite<Postava>(dbPath, "Postavy");
            spravceHer = new SpravceDatLite<Hra>(dbPath, "Hry");
        }

        public List<Rasa> GetRasy() => spravceRas.NactiVse();
        public List<Povolani> GetPovolani() => spravcePovolani.NactiVse();
        public List<Schopnost> GetSchopnosti() => spravceSchopnosti.NactiVse();
        public List<Postava> GetPostavy() => spravcePostav.NactiVse();
        public List<Hra> GetHry() => spravceHer.NactiVse();

        public Rasa? NajdiRasu(Guid id) => GetRasy().Find(r => r.Id == id);
        public Povolani? NajdiPovolani(Guid id) => GetPovolani().Find(p => p.Id == id);
        public Schopnost? NajdiSchopnost(Guid id) => GetSchopnosti().Find(s => s.Id == id);
        public Postava? NajdiPostavu(Guid id) => GetPostavy().Find(p => p.Id == id);
        public Hra? NajdiHru(Guid id) => GetHry().Find(h => h.Id == id);

        public Rasa? NajdiRasuPodleNazvu(string nazev) => GetRasy().Find(r => r.Nazev == nazev);
        public Povolani? NajdiPovolaniPodleNazvu(string nazev) => GetPovolani().Find(p => p.Nazev == nazev);
        public Schopnost? NajdiSchopnostPodleNazvu(string nazev) => GetSchopnosti().Find(s => s.Nazev == nazev);
        public Postava? NajdiPostavuPodleJmena(string jmeno) => GetPostavy().Find(p => p.Jmeno == jmeno);
        public Hra? NajdiHruPodleNazvu(string nazev) => GetHry().Find(h => h.Nazev == nazev);

        public bool VytvorRasu(string nazev, string popis, string? obrazek = null)
        {
            if (GetRasy().Any(r => r.Nazev.Equals(nazev, StringComparison.OrdinalIgnoreCase)))
                return false;
            spravceRas.Pridej(new Rasa(nazev, popis, obrazek));
            return true;
        }

        public bool VytvorPovolani(string nazev, string popis, string? obrazek = null)
        {
            if (GetPovolani().Any(p => p.Nazev.Equals(nazev, StringComparison.OrdinalIgnoreCase)))
                return false;
            spravcePovolani.Pridej(new Povolani(nazev, popis, obrazek));
            return true;
        }

        public bool VytvorSchopnost(string nazev, string typ, string popis, string? obrazek = null)
        {
            if (GetSchopnosti().Any(s => s.Nazev.Equals(nazev, StringComparison.OrdinalIgnoreCase)))
                return false;
            spravceSchopnosti.Pridej(new Schopnost(nazev, typ, popis, obrazek));
            return true;
        }

        public bool VytvorPostavu(string jmeno, Guid rasaId, Guid povolaniId,
            List<Guid> schopnostiIds, List<KeyValuePair<string, int>> staty, Guid hraId, string? obrazek = null)
        {
            if (GetPostavy().Where(h => h.Equals(hraId)).Any(p => p.Jmeno.Equals(jmeno, StringComparison.OrdinalIgnoreCase)))
                return false;
            spravcePostav.Pridej(new Postava(jmeno, rasaId, povolaniId, schopnostiIds, staty, hraId, obrazek));
            return true;
        }

        public bool VytvorHru(string nazev, string? popis = null, string? cesta = null)
        {
            if (GetHry().Any(h => h.Nazev.Equals(nazev, StringComparison.OrdinalIgnoreCase)))
                return false;
            spravceHer.Pridej(new Hra(nazev, popis, cesta));
            return true;
        }

        public void UpravPostavu(Postava postava) => spravcePostav.Aktualizuj(postava);
        public void UpravRasu(Rasa rasa) => spravceRas.Aktualizuj(rasa);
        public void UpravPovolani(Povolani povolani) => spravcePovolani.Aktualizuj(povolani);
        public void UpravSchopnost(Schopnost schopnost) => spravceSchopnosti.Aktualizuj(schopnost);
        public void UpravHru(Hra hra) => spravceHer.Aktualizuj(hra);

        public void SmazPostavu(Guid id) => spravcePostav.Smaz(p => p.Id == id);
        public void SmazRasu(Guid id) => spravceRas.Smaz(r => r.Id == id);
        public void SmazPovolani(Guid id) => spravcePovolani.Smaz(p => p.Id == id);
        public void SmazSchopnost(Guid id) => spravceSchopnosti.Smaz(s => s.Id == id);
        public void SmazHru(Guid id) => spravceHer.Smaz(h => h.Id == id);
    }
}
