using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe.Models;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe.ViewModels
{
    sealed class Terminals : Screen
    {
        private Terminals() : base("Encoding Stations", PackIconKind.MonitorMultiple)
        {
            ShortName = "Stations";
        }

        private static Terminals _instance;
        public static Terminals Instance => _instance ?? (_instance = new Terminals());

        private ListCollectionView _items;

        public ListCollectionView Items
        {
            get
            {
                if (_items != null) return _items;
                _items = (ListCollectionView)CollectionViewSource.GetDefaultView(Client.Cache);
                return _items;
            }
        }

        private ListCollectionView _logs;

        public ListCollectionView Logs
        {
            get
            {
                if (_logs != null) return _logs;
                _logs = (ListCollectionView) CollectionViewSource.GetDefaultView(TerminalLog.Cache);
                _logs.SortDescriptions.Add(new SortDescription("Time", ListSortDirection.Descending));
                _logs.Filter = FilterLog;
                TerminalLog.Cache.CollectionChanged += (sender, args) =>
                {
                    _logs.Filter = FilterLog;
                };
                return _logs;
            }
        }

        private bool FilterLog(object o)
        {
            var log = o as TerminalLog;
            if (log == null) return false;
            return log.TerminalId == ((Client) Items.CurrentItem)?.Id;
        }
    }
}
