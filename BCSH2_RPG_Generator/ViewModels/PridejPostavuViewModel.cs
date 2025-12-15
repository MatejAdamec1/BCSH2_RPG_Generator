using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using BCSH2_RPG_Generator.Views;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class PridejPostavuViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;
        private readonly Guid hraId;
        private readonly Guid? upravovanaId;

        [ObservableProperty]
        private string? jmeno;

        [ObservableProperty]
        private string? obrazekCesta;

        [ObservableProperty]
        private BitmapImage? obrazekZdroj;

        [ObservableProperty]
        private Rasa? vybranaRasa;

        [ObservableProperty]
        private Povolani? vybranePovolani;

        public ObservableCollection<Rasa> Rasy { get; }
        public ObservableCollection<Povolani> PovolaniSeznam { get; }
        public ObservableCollection<SchopnostItem> Schopnosti { get; }
        public ObservableCollection<StatItem> Staty { get; }

        public PridejPostavuViewModel(SpravceVseho spravce, Guid hraId, Guid? upravovanaId = null)
        {
            this.spravce = spravce;
            this.hraId = hraId;
            this.upravovanaId = upravovanaId;

            Rasy = new ObservableCollection<Rasa>(spravce.GetRasy());
            PovolaniSeznam = new ObservableCollection<Povolani>(spravce.GetPovolani());
            Schopnosti = new ObservableCollection<SchopnostItem>(
                spravce.GetSchopnosti().Select(s => new SchopnostItem { Schopnost = s, JeVybrana = false })
            );

            Staty = new ObservableCollection<StatItem>(new[]
            {
                new StatItem("Síla", 10),
                new StatItem("Obratnost", 10),
                new StatItem("Odolnost", 10),
                new StatItem("Inteligence", 10),
                new StatItem("Moudrost", 10),
                new StatItem("Charisma", 10)
            });

            if (upravovanaId.HasValue)
                NactiData();
        }

        partial void OnObrazekCestaChanged(string? value)
        {
            AktualizujObrazek();
        }

        [RelayCommand]
        private void Ulozit()
        {
            if (string.IsNullOrWhiteSpace(Jmeno) || VybranaRasa == null || VybranePovolani == null)
            {
                CustomMessageBox.Show("Vyplň všechna pole!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            var vybraneSchopnosti = Schopnosti.Where(s => s.JeVybrana).Select(s => s.Schopnost.Id).ToList();
            if (!vybraneSchopnosti.Any())
            {
                CustomMessageBox.Show("Vyber alespoň jednu schopnost!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            var staty = Staty.Select(s => new KeyValuePair<string, int>(s.Nazev, s.Hodnota)).ToList();

            if (upravovanaId == null)
            {
                if (!spravce.VytvorPostavu(Jmeno, VybranaRasa.Id, VybranePovolani.Id, vybraneSchopnosti, staty, hraId, ObrazekCesta))
                {
                    CustomMessageBox.Show("Postava s tímto jménem už existuje.", "Duplicitní jméno", CustomMessageBoxButtons.OK);
                    return;
                }
                CustomMessageBox.Show("Postava přidána!", "Hotovo");
            }
            else
            {
                var p = new Postava(Jmeno, VybranaRasa.Id, VybranePovolani.Id, vybraneSchopnosti, staty, hraId, ObrazekCesta);
                p.Id = upravovanaId.Value;
                spravce.UpravPostavu(p);
                CustomMessageBox.Show("Postava upravena.", "Hotovo");
            }

            ZavritOkno();
        }

        [RelayCommand]
        private void Zrusit()
        {
            ZavritOkno();
        }

        [RelayCommand]
        private void VybratObrazek()
        {
            var ofd = new OpenFileDialog { Filter = "Obrázky (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
            if (ofd.ShowDialog() == true)
                ObrazekCesta = ofd.FileName;
        }

        [RelayCommand]
        private void ZvysitStat(StatItem? stat)
        {
            ZmenitStat(stat, +1);
        }

        [RelayCommand]
        private void SnizitStat(StatItem? stat)
        {
            ZmenitStat(stat, -1);
        }

        private void NactiData()
        {
            var p = spravce.NajdiPostavu(upravovanaId!.Value);
            if (p == null) return;

            Jmeno = p.Jmeno;
            VybranaRasa = spravce.NajdiRasu(p.RasaPostavy);
            VybranePovolani = spravce.NajdiPovolani(p.PovolaniPostavy);
            ObrazekCesta = p.Obrazek;

            foreach (var schop in Schopnosti)
                schop.JeVybrana = p.SchopnostiPostavy.Contains(schop.Schopnost.Id);

            foreach (var stat in Staty)
            {
                var nalezeny = p.Staty.FirstOrDefault(s => s.Key == stat.Nazev);
                if (!nalezeny.Equals(default(KeyValuePair<string, int>)))
                    stat.Hodnota = nalezeny.Value;
            }
        }

        private void AktualizujObrazek()
        {
            if (string.IsNullOrWhiteSpace(ObrazekCesta) || !File.Exists(ObrazekCesta))
            {
                ObrazekZdroj = null;
                return;
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(ObrazekCesta);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            ObrazekZdroj = bmp;
        }

        private void ZmenitStat(StatItem? stat, int zmena)
        {
            if (stat == null) return;
            stat.Hodnota = Math.Clamp(stat.Hodnota + zmena, 0, 100);
        }

        private void ZavritOkno()
        {
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this)?.Close();
        }
    }

    public partial class SchopnostItem : ObservableObject
    {
        public Schopnost Schopnost { get; set; } = null!;

        [ObservableProperty]
        private bool jeVybrana;
    }

    public partial class StatItem : ObservableObject
    {
        public string Nazev { get; set; }

        [ObservableProperty]
        private int hodnota;

        public StatItem(string nazev, int hodnota)
        {
            Nazev = nazev;
            this.hodnota = hodnota;
        }
    }
}
