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
using Generator_RPG.Views;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class SpravaPovolaniViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;

        [ObservableProperty]
        private Povolani? vybranePovolani;

        [ObservableProperty]
        private string? novyNazev;

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

        public ObservableCollection<Povolani> PovolaniSeznam { get; }

        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nové";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranePovolani != null;
        public bool MuzeZpet => !rezimPridani;

        public SpravaPovolaniViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            PovolaniSeznam = new ObservableCollection<Povolani>(spravce.GetPovolani());

            if (PovolaniSeznam.Count > 0)
                VybranePovolani = PovolaniSeznam[0];
        }

        partial void OnVybranePovolaniChanged(Povolani? value)
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
                VybranePovolani = null;
                NovyNazev = NovyPopis = "";
                ObrazekCesta = "";
                AktualizovatStavyTlacitek();
                return;
            }

            if (string.IsNullOrWhiteSpace(NovyNazev))
            {
                CustomMessageBox.Show("Zadej název povolání!", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            if (spravce.NajdiPovolaniPodleNazvu(NovyNazev) != null)
            {
                CustomMessageBox.Show("Povolání s tímto názvem už existuje.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorPovolani(NovyNazev, NovyPopis ?? "", ulozitCestu))
            {
                CustomMessageBox.Show("Vytvoření se nepodařilo.", "Chyba", CustomMessageBoxButtons.OK);
                return;
            }

            ObnovitSeznam();
            VybranePovolani = PovolaniSeznam.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            CustomMessageBox.Show("Povolání přidáno!", "Hotovo");
        }

        [RelayCommand(CanExecute = nameof(CanSmazatNeboZrusit))]
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

            if (CustomMessageBox.Show($"Opravdu smazat povolání „{VybranePovolani.Nazev}“?",
                                "Potvrzení", CustomMessageBoxButtons.YesNo) != MessageBoxResult.Yes)
                return;

            spravce.SmazPovolani(VybranePovolani.Id);
            ObnovitSeznam();

            VybranePovolani = PovolaniSeznam.FirstOrDefault();
        }
        private bool CanSmazatNeboZrusit() => rezimPridani || VybranePovolani != null;

        [RelayCommand(CanExecute = nameof(MuzeUlozit))]
        private void Ulozit()
        {
            if (VybranePovolani == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                CustomMessageBox.Show("Název nemůže být prázdný.", "Upozornění", CustomMessageBoxButtons.OK);
                return;
            }

            var exist = spravce.NajdiPovolaniPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranePovolani.Id)
            {
                CustomMessageBox.Show("Jiné povolání už má stejný název.", "Duplicitní název", CustomMessageBoxButtons.OK);
                return;
            }

            var upravene = new Povolani(nazev, NovyPopis ?? "", ObrazekCesta);
            upravene.Id = VybranePovolani.Id;

            spravce.UpravPovolani(upravene);

            ObnovitSeznam();
            VybranePovolani = PovolaniSeznam.FirstOrDefault(x => x.Id == upravene.Id);

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
            PovolaniSeznam.Clear();
            foreach (var s in spravce.GetPovolani())
                PovolaniSeznam.Add(s);
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
