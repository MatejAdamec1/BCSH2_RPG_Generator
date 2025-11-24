using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace BCSH2_RPG_Generator.ViewModels
{
    public class HlavniOknoViewModel : INotifyPropertyChanged
    {
        private readonly SpravceVseho spravce;
        private readonly Guid hraId;

        public ObservableCollection<PostavaZobraz> Postavy { get; set; }

        public ObservableCollection<string> SchopnostiVybranePostavy { get; set; }
        public ObservableCollection<KeyValuePair<string, int>> StatyVybranePostavy { get; set; }

        public ObservableCollection<string> FilterRasa { get; set; }
        public ObservableCollection<string> FilterPovolani { get; set; }

        private string? vybranaRasa;
        public string? VybranaRasa
        {
            get => vybranaRasa;
            set { vybranaRasa = value; OnPropertyChanged(); }
        }

        private string? vybranePovolani;
        public string? VybranePovolani
        {
            get => vybranePovolani;
            set { vybranePovolani = value; OnPropertyChanged(); }
        }

        private PostavaZobraz? vybranaPostava;
        public PostavaZobraz? VybranaPostava
        {
            get => vybranaPostava;
            set
            {
                vybranaPostava = value;
                OnPropertyChanged();
                NaplnDetailPostavy();
                AktualizovatTlacitka();
            }
        }

        public bool MuzeZpet => true;

        public ICommand PridatCommand { get; }
        public ICommand UpravitCommand { get; }
        public ICommand SmazatCommand { get; }
        public ICommand FiltrovatCommand { get; }
        public ICommand ZpetCommand { get; }

        public HlavniOknoViewModel(SpravceVseho spravce, Guid hraId)
        {
            this.spravce = spravce;
            this.hraId = hraId;

            Postavy = new ObservableCollection<PostavaZobraz>();
            SchopnostiVybranePostavy = new ObservableCollection<string>();
            StatyVybranePostavy = new ObservableCollection<KeyValuePair<string, int>>();
            FilterRasa = new ObservableCollection<string>();
            FilterPovolani = new ObservableCollection<string>();

            FilterRasa.Add("Vše");
            foreach (var r in spravce.GetRasy()) FilterRasa.Add(r.Nazev);
            VybranaRasa = "Vše";

            FilterPovolani.Add("Vše");
            foreach (var p in spravce.GetPovolani()) FilterPovolani.Add(p.Nazev);
            VybranePovolani = "Vše";

            PridatCommand = new RelayCommand(_ => PridatPostavu());
            UpravitCommand = new RelayCommand(_ => UpravitPostavu(), _ => VybranaPostava != null);
            SmazatCommand = new RelayCommand(_ => SmazatPostavu(), _ => VybranaPostava != null);
            FiltrovatCommand = new RelayCommand(_ => FiltrovatPostavy());
            ZpetCommand = new RelayCommand(_ => ZavritOkno(), _ => MuzeZpet);

            FiltrovatPostavy();

            if (Postavy.Any())
                VybranaPostava = Postavy[0];
        }

        private void NaplnDetailPostavy()
        {
            SchopnostiVybranePostavy.Clear();
            StatyVybranePostavy.Clear();

            if (VybranaPostava == null) return;

            foreach (var schop in VybranaPostava.SchopnostiPostavy)
            {
                var s = spravce.NajdiSchopnost(schop);
                if (s != null)
                    SchopnostiVybranePostavy.Add(s.Nazev);
            }

            foreach (var stat in VybranaPostava.Staty)
                StatyVybranePostavy.Add(stat);
        }

        private void FiltrovatPostavy()
        {
            var vsechny = spravce.GetPostavy()
                .Where(p => p.HraId == hraId);

            var filtrovane = vsechny
                .Where(p =>
                {
                    var rasa = spravce.NajdiRasu(p.RasaPostavy)?.Nazev ?? "";
                    var povolani = spravce.NajdiPovolani(p.PovolaniPostavy)?.Nazev ?? "";

                    bool okRasa = VybranaRasa == "Vše" || rasa == VybranaRasa;
                    bool okPovolani = VybranePovolani == "Vše" || povolani == VybranePovolani;

                    return okRasa && okPovolani;
                })
                .Select(p => new PostavaZobraz
                {
                    Id = p.Id,
                    Jmeno = p.Jmeno,
                    Rasa = spravce.NajdiRasu(p.RasaPostavy)?.Nazev ?? "",
                    Povolani = spravce.NajdiPovolani(p.PovolaniPostavy)?.Nazev ?? "",
                    Obrazek = p.Obrazek,
                    Staty = p.Staty,
                    SchopnostiPostavy = p.SchopnostiPostavy
                })
                .ToList();

            Postavy.Clear();
            foreach (var p in filtrovane)
                Postavy.Add(p);

            if (Postavy.Any())
                VybranaPostava = Postavy[0];
        }

        private void PridatPostavu()
        {
            var view = new BCSH2_RPG_Generator.Views.PridejPostavuView();
            view.DataContext = new BCSH2_RPG_Generator.ViewModels.PridejPostavuViewModel(spravce, hraId);
            view.ShowDialog();
            FiltrovatPostavy(); 
        }

        private void UpravitPostavu()
        {
            if (VybranaPostava == null) return;

            var view = new BCSH2_RPG_Generator.Views.PridejPostavuView();
            view.DataContext = new BCSH2_RPG_Generator.ViewModels.PridejPostavuViewModel(spravce, hraId, VybranaPostava.Id);
            view.ShowDialog();
            FiltrovatPostavy(); 
        }

        private void SmazatPostavu()
        {
            if (VybranaPostava == null) return;

            var potvrdit = MessageBox.Show($"Opravdu smazat postavu „{VybranaPostava.Jmeno}“?",
                                           "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (potvrdit != MessageBoxResult.Yes) return;

            spravce.SmazPostavu(VybranaPostava.Id);
            FiltrovatPostavy(); 
        }

        private void ZavritOkno()
        {
            var currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            var okno = new BCSH2_RPG_Generator.Views.MainMenuView();
            currentWindow?.Close();
            okno.Show();
        }

        private void AktualizovatTlacitka()
        {
            (UpravitCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SmazatCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PostavaZobraz
    {
        public Guid Id { get; set; }
        public string Jmeno { get; set; } = "";
        public string Rasa { get; set; } = "";
        public string Povolani { get; set; } = "";
        public string? Obrazek { get; set; }
        public List<KeyValuePair<string, int>> Staty { get; set; } = new();
        public List<Guid> SchopnostiPostavy { get; set; } = new();
    }
}
