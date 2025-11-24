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
    public class SpravaPovolaniViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;

        private Povolani? vybranePovolani;
        private string? novyNazev;
        private string? novyPopis;
        private string? obrazekCesta;
        private BitmapImage? obrazekZdroj;

        private bool rezimPridani;
        private bool isGridEnabled = true;

        public ObservableCollection<Povolani> PovolaniSeznam { get; set; }

        public Povolani? VybranePovolani
        {
            get => vybranePovolani;
            set
            {
                vybranePovolani = value;
                if (!rezimPridani)
                {
                    if (value != null)
                    {
                        NovyNazev = value.Nazev;
                        NovyPopis = value.Popis;
                        ObrazekCesta = value.Obrazek;
                    }
                    else
                    {
                        NovyNazev = NovyPopis = ObrazekCesta = string.Empty;
                    }
                    AktualizujObrazek();
                }
                OnPropertyChanged();
                AktualizovatStavyTlacitek();
            }
        }

        public string? NovyNazev { get => novyNazev; set { novyNazev = value; OnPropertyChanged(); } }
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
        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nové";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranePovolani != null;
        public bool MuzeZpet => !rezimPridani;

        public ICommand NovaNeboPridatCommand { get; }
        public ICommand SmazatNeboZrusitCommand { get; }
        public ICommand UlozitCommand { get; }
        public ICommand VybratObrazekCommand { get; }
        public ICommand ZpetCommand { get; }

        public SpravaPovolaniViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            PovolaniSeznam = new ObservableCollection<Povolani>(spravce.GetPovolani());

            NovaNeboPridatCommand = new RelayCommand(_ => NovaNeboPridat());
            SmazatNeboZrusitCommand = new RelayCommand(_ => SmazatNeboZrusit(), _ => rezimPridani || VybranePovolani != null);
            UlozitCommand = new RelayCommand(_ => UlozitZmeny(), _ => MuzeUlozit);
            VybratObrazekCommand = new RelayCommand(_ => VyberObrazek());
            ZpetCommand = new RelayCommand(_ => ZavritOkno(), _ => MuzeZpet);

            if (PovolaniSeznam.Count > 0)
                VybranePovolani = PovolaniSeznam[0];
        }

        private void NovaNeboPridat()
        {
            if (!rezimPridani)
            {
                rezimPridani = true;
                IsGridEnabled = false;
                VybranePovolani = null;
                NovyNazev = NovyPopis = "";
                ObrazekCesta = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                MessageBox.Show("Zadej název povolání!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (spravce.NajdiPovolaniPodleNazvu(NovyNazev) != null)
            {
                MessageBox.Show("Povolání s tímto názvem už existuje.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorPovolani(NovyNazev, NovyPopis ?? "", ulozitCestu))
            {
                MessageBox.Show("Vytvoření se nepodařilo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ObnovitSeznam();
            VybranePovolani = PovolaniSeznam.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            MessageBox.Show("Povolání přidáno!", "Hotovo");
        }

        private void SmazatNeboZrusit()
        {
            if (rezimPridani)
            {
                rezimPridani = false;
                IsGridEnabled = true;
                if (PovolaniSeznam.Any())
                    VybranePovolani = PovolaniSeznam.First();
                AktualizovatStavyTlacitek();
                return;
            }

            if (VybranePovolani == null) return;

            if (MessageBox.Show($"Opravdu smazat povolání „{VybranePovolani.Nazev}“?",
                                "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            spravce.SmazPovolani(VybranePovolani.Id);
            ObnovitSeznam();

            VybranePovolani = PovolaniSeznam.FirstOrDefault();
        }

        private void UlozitZmeny()
        {
            if (VybranePovolani == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                MessageBox.Show("Název nemůže být prázdný.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exist = spravce.NajdiPovolaniPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranePovolani.Id)
            {
                MessageBox.Show("Jiné povolání už má stejný název.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var upravene = new Povolani(nazev, NovyPopis ?? "", ObrazekCesta);
            upravene.Id = VybranePovolani.Id;

            spravce.UpravPovolani(upravene);

            ObnovitSeznam();
            VybranePovolani = PovolaniSeznam.FirstOrDefault(x => x.Id == upravene.Id);

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
            PovolaniSeznam.Clear();
            foreach (var s in spravce.GetPovolani())
                PovolaniSeznam.Add(s);
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
