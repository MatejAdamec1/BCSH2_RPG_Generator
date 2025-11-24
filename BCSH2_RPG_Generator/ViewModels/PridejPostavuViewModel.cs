using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BCSH2_RPG_Generator.ViewModels
{
    public class PridejPostavuViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;
        private readonly Guid hraId;
        private readonly Guid? upravovanaId;

        private string? jmeno;
        private string? obrazekCesta;
        private BitmapImage? obrazekZdroj;
        private Rasa? vybranaRasa;
        private Povolani? vybranePovolani;

        public ObservableCollection<Rasa> Rasy { get; set; }
        public ObservableCollection<Povolani> PovolaniSeznam { get; set; }
        public ObservableCollection<SchopnostItem> Schopnosti { get; set; }
        public ObservableCollection<StatItem> Staty { get; set; }

        public string? Jmeno { get => jmeno; set { jmeno = value; OnPropertyChanged(); } }
        public Rasa? VybranaRasa { get => vybranaRasa; set { vybranaRasa = value; OnPropertyChanged(); } }
        public Povolani? VybranePovolani { get => vybranePovolani; set { vybranePovolani = value; OnPropertyChanged(); } }
        public string? ObrazekCesta { get => obrazekCesta; set { obrazekCesta = value; OnPropertyChanged(); AktualizujObrazek(); } }
        public BitmapImage? ObrazekZdroj { get => obrazekZdroj; set { obrazekZdroj = value; OnPropertyChanged(); } }

        public ICommand UlozitCommand { get; }
        public ICommand ZrusitCommand { get; }
        public ICommand VybratObrazekCommand { get; }
        public ICommand ZvysitStatCommand { get; }
        public ICommand SnizitStatCommand { get; }

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

            UlozitCommand = new RelayCommand(_ => Ulozit());
            ZrusitCommand = new RelayCommand(_ => ZavritOkno());
            VybratObrazekCommand = new RelayCommand(_ => VyberObrazek());
            ZvysitStatCommand = new RelayCommand(stat => ZmenitStat(stat as StatItem, +1));
            SnizitStatCommand = new RelayCommand(stat => ZmenitStat(stat as StatItem, -1));

            ObrazekCesta = "C:\\Users\\matej\\source\\repos\\Generator_RPG\\Generator_RPG\\Assets\\PictureBox UnknownbackTemp.png";

            if (upravovanaId.HasValue)
                NactiData();
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

        private void Ulozit()
        {
            ObrazekCesta = string.IsNullOrWhiteSpace(ObrazekCesta) ? "C:\\Users\\matej\\source\\repos\\Generator_RPG\\Generator_RPG\\Assets\\PictureBox UnknownbackTemp.png" : ObrazekCesta;
            if (string.IsNullOrWhiteSpace(Jmeno) || VybranaRasa == null || VybranePovolani == null)
            {
                MessageBox.Show("Vyplň všechna pole!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var vybraneSchopnosti = Schopnosti.Where(s => s.JeVybrana).Select(s => s.Schopnost.Id).ToList();
            if (!vybraneSchopnosti.Any())
            {
                MessageBox.Show("Vyber alespoň jednu schopnost!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var staty = Staty.Select(s => new KeyValuePair<string, int>(s.Nazev, s.Hodnota)).ToList();

            if (upravovanaId == null)
            {
                if (!spravce.VytvorPostavu(Jmeno, VybranaRasa.Id, VybranePovolani.Id, vybraneSchopnosti, staty, hraId, ObrazekCesta))
                {
                    MessageBox.Show("Postava s tímto jménem už existuje.", "Duplicitní jméno", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                MessageBox.Show("Postava přidána!", "Hotovo");
            }
            else
            {
                var p = new Postava(Jmeno, VybranaRasa.Id, VybranePovolani.Id, vybraneSchopnosti, staty, hraId, ObrazekCesta);
                p.Id = upravovanaId.Value;
                spravce.UpravPostavu(p);
                MessageBox.Show("Postava upravena.", "Hotovo");
            }

            ZavritOkno();
        }

        private void VyberObrazek()
        {
            var ofd = new OpenFileDialog { Filter = "Obrázky (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
            if (ofd.ShowDialog() == true)
                ObrazekCesta = ofd.FileName;
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SchopnostItem : INotifyPropertyChanged
    {
        public Schopnost Schopnost { get; set; } = null!;
        private bool jeVybrana;
        public bool JeVybrana { get => jeVybrana; set { jeVybrana = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class StatItem : INotifyPropertyChanged
    {
        public string Nazev { get; set; }
        private int hodnota;
        public int Hodnota { get => hodnota; set { hodnota = value; OnPropertyChanged(); } }

        public StatItem(string nazev, int hodnota) { Nazev = nazev; this.hodnota = hodnota; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
