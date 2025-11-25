using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class SpravaHerViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;

        [ObservableProperty]
        private Hra? vybranaHra;

        [ObservableProperty]
        private string? novyNazev;

        [ObservableProperty]
        private string? novyPopis;

        [ObservableProperty]
        private bool rezimPridani;

        [ObservableProperty]
        private bool isGridEnabled = true;

        public ObservableCollection<Hra> Hry { get; }

        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaHra != null;
        public bool MuzeZpet => !rezimPridani;

        public SpravaHerViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Hry = new ObservableCollection<Hra>(spravce.GetHry());

            if (Hry.Count > 0)
                VybranaHra = Hry[0];
        }

        partial void OnVybranaHraChanged(Hra? value)
        {
            if (!rezimPridani)
            {
                if (value != null)
                {
                    NovyNazev = value.Nazev;
                    NovyPopis = value.Popis;
                }
                else
                {
                    NovyNazev = NovyPopis = "";
                }
            }
            AktualizovatStavyTlacitek();
        }

        [RelayCommand]
        private void OtevritSlozku()
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

        [RelayCommand]
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

        [RelayCommand(CanExecute = nameof(CanSmazatNeboZrusit))]
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
        private bool CanSmazatNeboZrusit() => rezimPridani || VybranaHra != null;

        [RelayCommand(CanExecute = nameof(MuzeUlozit))]
        private void Ulozit()
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

        [RelayCommand]
        private void VybratCestu()
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

        [RelayCommand(CanExecute = nameof(MuzeZpet))]
        private void Zpet()
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

            SmazatNeboZrusitCommand.NotifyCanExecuteChanged();
            UlozitCommand.NotifyCanExecuteChanged();
            ZpetCommand.NotifyCanExecuteChanged();
        }
    }
}
