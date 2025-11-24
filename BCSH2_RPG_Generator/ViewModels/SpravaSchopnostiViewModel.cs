using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using Microsoft.Win32;
using System;
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
    public class SpravaSchopnostiViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;

        private Schopnost? vybranaSchopnost;
        private string? novyNazev;
        private string? novyTyp;
        private string? novyPopis;
        private string? obrazekCesta;
        private BitmapImage? obrazekZdroj;

        private bool rezimPridani;
        private bool isGridEnabled = true;

        public ObservableCollection<Schopnost> Schopnosti { get; set; }

        public Schopnost? VybranaSchopnost
        {
            get => vybranaSchopnost;
            set
            {
                vybranaSchopnost = value;
                if (!rezimPridani)
                {
                    if (value != null)
                    {
                        NovyNazev = value.Nazev;
                        NovyTyp = value.Typ;
                        NovyPopis = value.Popis;
                        ObrazekCesta = value.Obrazek;
                    }
                    else
                    {
                        NovyNazev = NovyTyp = NovyPopis = ObrazekCesta = string.Empty;
                    }
                    AktualizujObrazek();
                }
                OnPropertyChanged();
                AktualizovatStavyTlacitek();
            }
        }

        public string? NovyNazev { get => novyNazev; set { novyNazev = value; OnPropertyChanged(); } }
        public string? NovyTyp { get => novyTyp; set { novyTyp = value; OnPropertyChanged(); } }
        public string? NovyPopis { get => novyPopis; set { novyPopis = value; OnPropertyChanged(); } }
        public string? ObrazekCesta
        {
            get => obrazekCesta;
            set { obrazekCesta = value; OnPropertyChanged(); AktualizujObrazek(); }
        }

        public BitmapImage? ObrazekZdroj
        {
            get => obrazekZdroj;
            set { obrazekZdroj = value; OnPropertyChanged(); }
        }

        public bool IsGridEnabled { get => isGridEnabled; set { isGridEnabled = value; OnPropertyChanged(); } }
        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaSchopnost != null;
        public bool MuzeZpet => !rezimPridani;

        public ICommand NovaNeboPridatCommand { get; }
        public ICommand SmazatNeboZrusitCommand { get; }
        public ICommand UlozitCommand { get; }
        public ICommand VybratObrazekCommand { get; }
        public ICommand ZpetCommand { get; }

        public SpravaSchopnostiViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Schopnosti = new ObservableCollection<Schopnost>(spravce.GetSchopnosti());

            NovaNeboPridatCommand = new RelayCommand(_ => NovaNeboPridat());
            SmazatNeboZrusitCommand = new RelayCommand(_ => SmazatNeboZrusit(), _ => rezimPridani || VybranaSchopnost != null);
            UlozitCommand = new RelayCommand(_ => UlozitZmeny(), _ => MuzeUlozit);
            VybratObrazekCommand = new RelayCommand(_ => VyberObrazek());
            ZpetCommand = new RelayCommand(_ => ZavritOkno(), _ => MuzeZpet);

            if (Schopnosti.Count > 0)
                VybranaSchopnost = Schopnosti[0];
        }

        private void NovaNeboPridat()
        {
            if (!rezimPridani)
            {
                rezimPridani = true;
                IsGridEnabled = false;
                VybranaSchopnost = null;
                NovyNazev = NovyTyp = NovyPopis = "";
                ObrazekCesta = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                MessageBox.Show("Zadej název schopnosti!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (spravce.NajdiSchopnostPodleNazvu(NovyNazev) != null)
            {
                MessageBox.Show("Schopnost s tímto názvem už existuje.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorSchopnost(NovyNazev, NovyTyp ?? "", NovyPopis ?? "", ulozitCestu))
            {
                MessageBox.Show("Vytvoření se nepodařilo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ObnovitSeznam();
            VybranaSchopnost = Schopnosti.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            MessageBox.Show("Schopnost přidána!", "Hotovo");
        }

        private void SmazatNeboZrusit()
        {
            if (rezimPridani)
            {
                rezimPridani = false;
                IsGridEnabled = true;
                if (Schopnosti.Any())
                    VybranaSchopnost = Schopnosti.First();
                AktualizovatStavyTlacitek();
                return;
            }

            if (VybranaSchopnost == null) return;

            if (MessageBox.Show($"Opravdu smazat schopnost „{VybranaSchopnost.Nazev}“?",
                                "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            spravce.SmazSchopnost(VybranaSchopnost.Id);
            ObnovitSeznam();

            VybranaSchopnost = Schopnosti.FirstOrDefault();
        }

        private void UlozitZmeny()
        {
            if (VybranaSchopnost == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                MessageBox.Show("Název nemůže být prázdný.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exist = spravce.NajdiSchopnostPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaSchopnost.Id)
            {
                MessageBox.Show("Jiná schopnost už má stejný název.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var upravena = new Schopnost(nazev, NovyTyp ?? "", NovyPopis ?? "", ObrazekCesta);
            upravena.Id = VybranaSchopnost.Id;

            spravce.UpravSchopnost(upravena);

            ObnovitSeznam();
            VybranaSchopnost = Schopnosti.FirstOrDefault(x => x.Id == upravena.Id);

            MessageBox.Show("Změny byly uloženy.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AktualizujObrazek()
        {
            string? path = ObrazekCesta;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ObrazekZdroj = null;
                return;
            }

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                ObrazekZdroj = bmp;
            }
            catch
            {
                ObrazekZdroj = null;
            }
        }

        private void VyberObrazek()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Obrázky (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Všechny soubory (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
                ObrazekCesta = ofd.FileName;
        }

        private void ZavritOkno()
        {
            if (rezimPridani)
            {
                MessageBox.Show("Nejprve dokonči nebo zruš přidání.", "Upozornění",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            if (currentWindow == null)
                return;

            var menu = new Views.MainMenuView();

            menu.Show();

            currentWindow.Close();
        }

        private void ObnovitSeznam()
        {
            Schopnosti.Clear();
            foreach (var s in spravce.GetSchopnosti())
                Schopnosti.Add(s);
        }

        private void AktualizovatStavyTlacitek()
        {
            OnPropertyChanged(nameof(TextTlacitkoNova));
            OnPropertyChanged(nameof(TextTlacitkoSmazat));
            OnPropertyChanged(nameof(MuzeUlozit));
            OnPropertyChanged(nameof(MuzeZpet));
            OnPropertyChanged(nameof(IsGridEnabled));

            (SmazatNeboZrusitCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UlozitCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ZpetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
