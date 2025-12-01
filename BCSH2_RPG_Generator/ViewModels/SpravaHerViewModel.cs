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
using Generator_RPG.Views;

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

            var okno = new Views.HlavniOknoView
            {
                DataContext = new HlavniOknoViewModel(spravce, VybranaHra.Id)
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
                CustomMessageBox.Show("Zadej název hry!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            if (spravce.NajdiHruPodleNazvu(NovyNazev) != null)
            {
                CustomMessageBox.Show("Hra s tímto názvem už existuje.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            if (!spravce.VytvorHru(NovyNazev, NovyPopis ?? ""))
            {
                CustomMessageBox.Show("Vytvoření hry se nepodařilo.", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            ObnovitSeznam();
            VybranaHra = Hry.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            CustomMessageBox.Show("Hra přidána!", "Hotovo");
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

            if (CustomMessageBox.Show($"Opravdu smazat hru „{VybranaHra.Nazev}“?",
                                "Potvrzení", CustomMessageBoxButtons.YesNo) != MessageBoxResult.Yes)
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
                CustomMessageBox.Show("Název nemůže být prázdný.", "Upozornění", CustomMessageBoxButtons.OK);
                return;
            }

            var exist = spravce.NajdiHruPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaHra.Id)
            {
                CustomMessageBox.Show("Jiná hra už má stejný název.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            var upravena = new Hra(nazev, NovyPopis ?? "");
            upravena.Id = VybranaHra.Id;

            spravce.UpravHru(upravena);

            ObnovitSeznam();
            VybranaHra = Hry.FirstOrDefault(x => x.Id == upravena.Id);

            CustomMessageBox.Show("Změny byly uloženy.", "Hotovo", CustomMessageBoxButtons.OK);
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
                CustomMessageBox.Show("Nejprve dokonči nebo zruš přidání.", "Upozornění",
                                CustomMessageBoxButtons.OK);
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
