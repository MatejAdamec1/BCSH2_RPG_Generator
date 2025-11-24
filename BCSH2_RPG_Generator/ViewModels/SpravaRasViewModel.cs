using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using BCSH2_RPG_Generator.Views;
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
    /*
     do ONpropertyChanged dát (nameof(to jméno)) -> nemusí to bejt string
     vytvořit RelayCommand pro ICommand implementaci
           tam si připravím tu OnExecuteChanged
           udělám si tam delegate typu Action, kterej se jmenuje _execute a metodu Execute, která zavolá ten delegate
           dám delegát Func<bool> _canExecute a metodu CanExecute, která zavolá ten delegate

     ve ViewModelu si místo ICommand vytvořím RelayCommand v konstruktoru dám lambdu na metodu, kterou má provést
        např pro přidávání rasy -> RelayCommand(_ => PridatRasu())


    volání OnPropertyChanged
         vytvořím si něco jako základní ViewModel, kde bude implementace INotifyPropertyChanged
         a tu metodu s OnPropertyChanged si tam taky dám a zbytek VM bude dědit z toho základního VM
         parametry -> ([CallerMemberName] string? property = "") -> v jednotlivých setterech to jde volat bez parametru

    ---------
     produkční řešení balíček CommunityToolkit.Mvvm od Microsoftu (jde i v komerčních projektech)
         místo předka BaseViewModel si dám ObservableObject (nebo to anotuju [ObservableObject] a dám si třídu partial)
         a najednou mám metody OnPropertyChanged a OnPropertyChanging
         jako sledování selected mi jde dát [ObservableProperty] private Rasa? vybranaRasa, pokud to anotuju ještě 
             [NotifyPropertyChangedFor(nameof(NaprRemoveCommand))]
         a dám [RelayCommand] nad metodu, která má být příkazem, nad to pak můžu dát [RelayCommand(CanExecute = nameof(MuzeRemove))], 
             pak stačí jen vytvořit bool MuzeRemove() => VybranaRasa != null;
         z atributů je [ObservableProperty], z těch commandů je [RelayCommand]

    */

    public class SpravaRasViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;

        private Rasa? vybranaRasa;
        private string? novyNazev;
        private string? novyPopis;
        private string? obrazekCesta;
        private BitmapImage? obrazekZdroj;

        private bool rezimPridani = false;
        private bool isGridEnabled = true;

        public ObservableCollection<Rasa> Rasy { get; set; }

        public Rasa? VybranaRasa
        {
            get => vybranaRasa;
            set
            {
                vybranaRasa = value;
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
        public string TextTlacitkoNova => rezimPridani ? "Přidat" : "Nová";
        public string TextTlacitkoSmazat => rezimPridani ? "Zrušit" : "Smazat";
        public bool MuzeUlozit => !rezimPridani && VybranaRasa != null;
        public bool MuzeZpet => !rezimPridani;

        public ICommand NovaNeboPridatCommand { get; }
        public ICommand SmazatNeboZrusitCommand { get; }
        public ICommand UlozitCommand { get; }
        public ICommand VybratObrazekCommand { get; }
        public ICommand ZpetCommand { get; }

        public SpravaRasViewModel()
        {
            string cestaDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rpg.db");
            spravce = new SpravceVseho(cestaDb);

            Rasy = new ObservableCollection<Rasa>(spravce.GetRasy());

            NovaNeboPridatCommand = new RelayCommand(_ => NovaNeboPridat());
            SmazatNeboZrusitCommand = new RelayCommand(_ => SmazatNeboZrusit(), _ => rezimPridani || VybranaRasa != null);
            UlozitCommand = new RelayCommand(_ => UlozitZmeny(), _ => MuzeUlozit);
            VybratObrazekCommand = new RelayCommand(_ => VyberObrazek());
            ZpetCommand = new RelayCommand(_ => ZavritOkno(), _ => MuzeZpet);

            if (Rasy.Count > 0)
                VybranaRasa = Rasy[0];
        }

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
                MessageBox.Show("Zadej název rasy!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (spravce.NajdiRasuPodleNazvu(NovyNazev) != null)
            {
                MessageBox.Show("Rasa s tímto názvem už existuje.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ulozitCestu = string.IsNullOrWhiteSpace(ObrazekCesta) ? "" : ObrazekCesta;

            if (!spravce.VytvorRasu(NovyNazev, NovyPopis ?? "", ulozitCestu))
            {
                MessageBox.Show("Vytvoření se nepodařilo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ObnovitSeznam();
            VybranaRasa = Rasy.LastOrDefault();

            rezimPridani = false;
            IsGridEnabled = true;
            AktualizovatStavyTlacitek();

            MessageBox.Show("Rasa přidána!", "Hotovo");
        }

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

            if (MessageBox.Show($"Opravdu smazat rasu „{VybranaRasa.Nazev}“?",
                                "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            spravce.SmazRasu(VybranaRasa.Id);
            ObnovitSeznam();

            VybranaRasa = Rasy.FirstOrDefault();
        }

        private void UlozitZmeny()
        {
            if (VybranaRasa == null) return;

            var nazev = (NovyNazev ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nazev))
            {
                MessageBox.Show("Název nemůže být prázdný.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exist = spravce.NajdiRasuPodleNazvu(nazev);
            if (exist != null && exist.Id != VybranaRasa.Id)
            {
                MessageBox.Show("Jiná rasa už má stejný název.", "Duplicitní název", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var upravena = new Rasa(nazev, NovyPopis ?? "", ObrazekCesta);
            upravena.Id = VybranaRasa.Id;

            spravce.UpravRasu(upravena);

            ObnovitSeznam();
            VybranaRasa = Rasy.FirstOrDefault(x => x.Id == upravena.Id);

            MessageBox.Show("Změny byly uloženy.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
            AktualizovatStavyTlacitek();
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
            Rasy.Clear();
            foreach (var s in spravce.GetRasy())
                Rasy.Add(s);
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
