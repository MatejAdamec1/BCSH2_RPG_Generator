using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BCSH2_RPG_Generator.Views;

namespace BCSH2_RPG_Generator.ViewModels
{
    public partial class HlavniOknoViewModel : ObservableObject
    {
        private readonly SpravceVseho spravce;
        private readonly Guid hraId;

        public ObservableCollection<PostavaZobraz> Postavy { get; }
        public ObservableCollection<string> SchopnostiVybranePostavy { get; }
        public ObservableCollection<KeyValuePair<string, int>> StatyVybranePostavy { get; }
        public ObservableCollection<string> FilterRasa { get; }
        public ObservableCollection<string> FilterPovolani { get; }

        [ObservableProperty]
        private string? vybranaRasa;

        [ObservableProperty]
        private string? vybranePovolani;

        [ObservableProperty]
        private PostavaZobraz? vybranaPostava;

        public bool MuzeZpet => true;

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

            FiltrovatPostavy();

            if (Postavy.Any())
                VybranaPostava = Postavy[0];
        }

        partial void OnVybranaPostavaChanged(PostavaZobraz? value)
        {
            NaplnDetailPostavy();
            AktualizovatTlacitka();
        }

        [RelayCommand]
        private void Pridat()
        {
            var view = new BCSH2_RPG_Generator.Views.PridejPostavuView();
            view.DataContext = new BCSH2_RPG_Generator.ViewModels.PridejPostavuViewModel(spravce, hraId);
            view.ShowDialog();
            FiltrovatPostavy();
        }

        [RelayCommand(CanExecute = nameof(CanUpravit))]
        private void Upravit()
        {
            if (VybranaPostava == null) return;

            var view = new PridejPostavuView();
            view.DataContext = new PridejPostavuViewModel(spravce, hraId, VybranaPostava.Id);
            view.ShowDialog();
            FiltrovatPostavy();
        }
        private bool CanUpravit() => VybranaPostava != null;

        [RelayCommand(CanExecute = nameof(CanSmazat))]
        private void Smazat()
        {
            if (VybranaPostava == null) return;

            if (CustomMessageBox.Show($"Opravdu smazat postavu „{VybranaPostava.Jmeno}“?",
                                           "Potvrzení", CustomMessageBoxButtons.YesNo) != MessageBoxResult.Yes) 
                return;

            spravce.SmazPostavu(VybranaPostava.Id);
            FiltrovatPostavy();
        }
        private bool CanSmazat() => VybranaPostava != null;

        [RelayCommand]
        private void Filtrovat()
        {
            FiltrovatPostavy();
        }

        [RelayCommand(CanExecute = nameof(MuzeZpet))]
        private void Zpet()
        {
            var currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            var okno = new BCSH2_RPG_Generator.Views.MainMenuView();
            currentWindow?.Close();
            okno.Show();
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

        private void AktualizovatTlacitka()
        {
            UpravitCommand.NotifyCanExecuteChanged();
            SmazatCommand.NotifyCanExecuteChanged();
        }
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
