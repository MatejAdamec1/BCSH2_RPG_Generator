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

namespace BCSH2_RPG_Generator.ViewModels
{
    public class SpravaHerViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;

        private Hra? vybranaHra;
        private string? novyNazev;
        private string? novyPopis;

        private bool rezimPridani;
        private bool isGridEnabled = true;

        public ObservableCollection<Hra> Hry { get; set; }

        public Hra? VybranaHra
        {
            get => vybranaHra;
            set
            {
                vybranaHra = value;
                if (!rezimPridani)
                {
                    if (value != null)
                    {
                        NovyNazev = value.Nazev;
                        NovyPopis = value.Popis;
                    }
                    else
                    {
                        NovyNazev = NovyPopis;
                    }
                }
                OnPropertyChanged();
                AktualizovatStavyTlacitek();
            }
        }

        public string? NovyNazev { get => novyNazev; set { novyNazev = value; OnPropertyChanged(); } }
        public string? NovyPopis { get => novyPopis; set { novyPopis = value; OnPropertyChanged(); } }

        public bool IsGridEnabled { get => isGridEnabled; set { isGridEnabled = value; OnPropertyChanged(); } }
        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaHra != null;
        public bool MuzeZpet => !rezimPridani;

        public ICommand NovaNeboPridatCommand { get; }
        public ICommand SmazatNeboZrusitCommand { get; }
        public ICommand UlozitCommand { get; }
        public ICommand VybratCestuCommand { get; }
        public ICommand ZpetCommand { get; }

        public ICommand OtevritSlozkuCommand { get; }

        public SpravaHerViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Hry = new ObservableCollection<Hra>(spravce.GetHry());

            NovaNeboPridatCommand = new RelayCommand(_ => NovaNeboPridat());
            SmazatNeboZrusitCommand = new RelayCommand(_ => SmazatNeboZrusit(), _ => rezimPridani || VybranaHra != null);
            UlozitCommand = new RelayCommand(_ => UlozitZmeny(), _ => MuzeUlozit);
            VybratCestuCommand = new RelayCommand(_ => VyberCestu());
            ZpetCommand = new RelayCommand(_ => ZavritOkno(), _ => MuzeZpet);
            OtevritSlozkuCommand = new RelayCommand(_ => OtevritHru());

            if (Hry.Count > 0)
                VybranaHra = Hry[0];
        }

        private void OtevritHru()
        {
            if (VybranaHra == null) return;

            var currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            var okno = new BCSH2_RPG_Generator.Views.HlavniOknoView
            {
                DataContext = new BCSH2_RPG_Generator.ViewModels.HlavniOknoViewModel(spravce, VybranaHra.Id)
            };

            currentWindow?.Close();

            okno.Show();
        }

        private void NovaNeboPridat()
        {
            if (!rezimPridani)
            {
                rezimPridani = true;
                IsGridEnabled = false;
                VybranaHra = null;
                NovyNazev = NovyPopis = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                MessageBox.Show("Zadej název hry!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (spravce.NajdiHruPodleNazvu(NovyNazev) != null)
            {
                MessageBox.Show("Hra s tímto názvem už existuje.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!spravce.VytvorHru(NovyNazev, NovyPopis ?? ""))
            {
                MessageBox.Show("Vytvoření hry se nepodařilo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ObnovitSeznam();
            VybranaHra = Hry.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            MessageBox.Show("Hra přidána!", "Hotovo");
        }

        private void SmazatNeboZrusit()
        {
            if (rezimPridani)
            {
                rezimPridani = false;
                IsGridEnabled = true;
                if (Hry.Any())
                    VybranaHra = Hry.First();
                AktualizovatStavyTlacitek();
                return;
            }

            if (VybranaHra == null) return;

            if (MessageBox.Show($"Opravdu smazat hru „{VybranaHra.Nazev}“?",
                                "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            spravce.SmazHru(VybranaHra.Id);
            ObnovitSeznam();

            VybranaHra = Hry.FirstOrDefault();
        }

        private void UlozitZmeny()
        {
            if (VybranaHra == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                MessageBox.Show("Název nemůže být prázdný.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exist = spravce.NajdiHruPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaHra.Id)
            {
                MessageBox.Show("Jiná hra už má stejný název.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var upravena = new Hra(nazev, NovyPopis ?? "");
            upravena.Id = VybranaHra.Id;

            spravce.UpravHru(upravena);

            ObnovitSeznam();
            VybranaHra = Hry.FirstOrDefault(x => x.Id == upravena.Id);

            MessageBox.Show("Změny byly uloženy.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void VyberCestu()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Vyber složku pro data hry",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Vyber složku..."
            };

            if (ofd.ShowDialog() == true)
            {
                string? selectedPath = Path.GetDirectoryName(ofd.FileName);
            }
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
            Hry.Clear();
            foreach (var h in spravce.GetHry())
                Hry.Add(h);
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
