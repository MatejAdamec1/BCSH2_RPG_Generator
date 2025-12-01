using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using BCSH2_RPG_Generator.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Generator_RPG.Views;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class SpravaRasViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;

        [ObservableProperty]
        private Rasa? vybranaRasa;

        [ObservableProperty]
        private string? novyNazev;

        [ObservableProperty]
        private string? novyPopis;

        [ObservableProperty]
        private string? obrazekCesta;

        [ObservableProperty]
        private BitmapImage? obrazekZdroj;

        [ObservableProperty]
        private bool rezimPridani = false;

        [ObservableProperty]
        private bool isGridEnabled = true;

        public ObservableCollection<Rasa> Rasy { get; }

        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaRasa != null;
        public bool MuzeZpet => !rezimPridani;

        public SpravaRasViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Rasy = new ObservableCollection<Rasa>(spravce.GetRasy());

            if (Rasy.Count > 0)
                VybranaRasa = Rasy[0];
        }

        partial void OnVybranaRasaChanged(Rasa? value)
        {
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
            AktualizovatStavyTlacitek();
        }

        partial void OnObrazekCestaChanged(string? value)
        {
            AktualizujObrazek();
        }

        [RelayCommand]
        private void NovaNeboPridat()
        {
            if (!rezimPridani)
            {
                rezimPridani = true;
                IsGridEnabled = false;
                VybranaRasa = null;
                NovyNazev = NovyPopis = "";
                ObrazekCesta = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                CustomMessageBox.Show("Zadej název rasy!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            if (spravce.NajdiRasuPodleNazvu(NovyNazev) != null)
            {
                CustomMessageBox.Show("Rasa s tímto názvem už existuje.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorRasu(NovyNazev, NovyPopis ?? "", ulozitCestu))
            {
                CustomMessageBox.Show("Vytvoření se nepodařilo.", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            ObnovitSeznam();
            VybranaRasa = Rasy.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            CustomMessageBox.Show("Rasa přidána!", "Hotovo");
        }

        [RelayCommand(CanExecute = nameof(CanSmazatNeboZrusit))]
        private void SmazatNeboZrusit()
        {
            if (rezimPridani)
            {
                rezimPridani = false;
                IsGridEnabled = true;
                if (Rasy.Any())
                    VybranaRasa = Rasy.First();
                AktualizovatStavyTlacitek();
                return;
            }

            if (VybranaRasa == null) return;

            if (CustomMessageBox.Show($"Opravdu smazat rasu „{VybranaRasa.Nazev}“?",
                                "Potvrzení", CustomMessageBoxButtons.YesNo) != MessageBoxResult.Yes)
                return;

            spravce.SmazRasu(VybranaRasa.Id);
            ObnovitSeznam();

            VybranaRasa = Rasy.FirstOrDefault();
        }
        private bool CanSmazatNeboZrusit() => rezimPridani || VybranaRasa != null;

        [RelayCommand(CanExecute = nameof(MuzeUlozit))]
        private void Ulozit()
        {
            if (VybranaRasa == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                CustomMessageBox.Show("Název nemůže být prázdný.", "Upozornění", CustomMessageBoxButtons.OK);
                return;
            }

            var exist = spravce.NajdiRasuPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaRasa.Id)
            {
                CustomMessageBox.Show("Jiná rasa už má stejný název.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            var upravena = new Rasa(nazev, NovyPopis ?? "", ObrazekCesta);
            upravena.Id = VybranaRasa.Id;

            spravce.UpravRasu(upravena);

            ObnovitSeznam();
            VybranaRasa = Rasy.FirstOrDefault(x => x.Id == upravena.Id);

            CustomMessageBox.Show("Změny byly uloženy.", "Hotovo", CustomMessageBoxButtons.OK);
            AktualizovatStavyTlacitek();
        }

        [RelayCommand]
        private void VybratObrazek()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Obrázky (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Všechny soubory (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
                ObrazekCesta = ofd.FileName;
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

            var menu = new MainMenuView();

            menu.Show();

            currentWindow.Close();
        }

        private void ObnovitSeznam()
        {
            Rasy.Clear();
            foreach (var s in spravce.GetRasy())
                Rasy.Add(s);
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
