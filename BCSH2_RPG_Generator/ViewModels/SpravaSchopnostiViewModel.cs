using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BCSH2_RPG_Generator.Views;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class SpravaSchopnostiViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;

        [ObservableProperty]
        private Schopnost? vybranaSchopnost;

        [ObservableProperty]
        private string? novyNazev;

        [ObservableProperty]
        private string? novyTyp;

        [ObservableProperty]
        private string? novyPopis;

        [ObservableProperty]
        private string? obrazekCesta;

        [ObservableProperty]
        private BitmapImage? obrazekZdroj;

        [ObservableProperty]
        private bool rezimPridani;

        [ObservableProperty]
        private bool isGridEnabled = true;

        public ObservableCollection<Schopnost> Schopnosti { get; }

        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaSchopnost != null;
        public bool MuzeZpet => !rezimPridani;

        public SpravaSchopnostiViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Schopnosti = new ObservableCollection<Schopnost>(spravce.GetSchopnosti());

            if (Schopnosti.Count > 0)
                VybranaSchopnost = Schopnosti[0];
        }

        partial void OnVybranaSchopnostChanged(Schopnost? value)
        {
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
                VybranaSchopnost = null;
                NovyNazev = NovyTyp = NovyPopis = "";
                ObrazekCesta = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                CustomMessageBox.Show("Zadej název schopnosti!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            if (spravce.NajdiSchopnostPodleNazvu(NovyNazev) != null)
            {
                CustomMessageBox.Show("Schopnost s tímto názvem už existuje.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorSchopnost(NovyNazev, NovyTyp ?? "", NovyPopis ?? "", ulozitCestu))
            {
                CustomMessageBox.Show("Vytvoření se nepodařilo.", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            ObnovitSeznam();
            VybranaSchopnost = Schopnosti.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            CustomMessageBox.Show("Schopnost přidána!", "Hotovo");
        }

        [RelayCommand(CanExecute = nameof(CanSmazatNeboZrusit))]
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

            if (CustomMessageBox.Show($"Opravdu smazat schopnost „{VybranaSchopnost.Nazev}“?",
                                "Potvrzení", CustomMessageBoxButtons.YesNo) != MessageBoxResult.Yes)
                return;

            spravce.SmazSchopnost(VybranaSchopnost.Id);
            ObnovitSeznam();

            VybranaSchopnost = Schopnosti.FirstOrDefault();
        }
        private bool CanSmazatNeboZrusit() => rezimPridani || VybranaSchopnost != null;

        [RelayCommand(CanExecute = nameof(MuzeUlozit))]
        private void Ulozit()
        {
            if (VybranaSchopnost == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                CustomMessageBox.Show("Název nemůže být prázdný.", "Upozornění", CustomMessageBoxButtons.OK);
                return;
            }

            var exist = spravce.NajdiSchopnostPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaSchopnost.Id)
            {
                CustomMessageBox.Show("Jiná schopnost už má stejný název.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            var upravena = new Schopnost(nazev, NovyTyp ?? "", NovyPopis ?? "", ObrazekCesta);
            upravena.Id = VybranaSchopnost.Id;

            spravce.UpravSchopnost(upravena);

            ObnovitSeznam();
            VybranaSchopnost = Schopnosti.FirstOrDefault(x => x.Id == upravena.Id);

            CustomMessageBox.Show("Změny byly uloženy.", "Hotovo", CustomMessageBoxButtons.OK);
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
