using BCSH2_RPG_Generator.Data;
using BCSH2_RPG_Generator.Spravce;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCSH2_RPG_Generator.ViewModels
{
    internal class MainViewModel
    {
        private readonly SpravceVseho spravce;
        public ObservableCollection<Postava> Postavy { get; set; }

        public MainViewModel()
        {
        }
    }
}
