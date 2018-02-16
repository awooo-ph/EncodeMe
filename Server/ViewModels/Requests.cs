using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class Requests : Screen
    {
        private Requests() : base("Enrollment Requests", PackIconKind.Bell)
        {
            ShortName = "Requests";
        }

        private static Requests _instance;
        public static Requests Instance => _instance ?? (_instance = new Requests());

        private ListCollectionView _items;
        public ListCollectionView Items => _items ?? (_items = new ListCollectionView(Request.Cache));
    }
}
