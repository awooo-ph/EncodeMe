﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class Requests : Screen
    {
        private Requests() : base("Enrollment Requests", PackIconKind.Bell)
        {
            ShortName = "Requests";
            
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            Commands.Add(new ScreenMenu("DELETE ALL", PackIconKind.DeleteSweep, DeleteAllCommand));

            Messenger.Default.AddListener<Models.Request>(Messages.RequestUpdated, req =>
            {
                Items.Filter = FilterRequest;
            });
        }

        private static ICommand _deleteAllCommand;

        private ICommand DeleteAllCommand => _deleteAllCommand ?? (_deleteAllCommand = new DelegateCommand(async d =>
        {
            await DialogHost.Show(new MessageDialog()
            {
                Icon = PackIconKind.DeleteForever,
                Title = "CONFIRM DELETE",
                Message = "Are you sure you want to delete all requests?\nThis action cannot be undone.",
                Affirmative = "DELETE ALL",
                Negative = "CANCEL",
                AffirmativeCommand = new DelegateCommand(dd =>
                {
                    Models.RequestDetail.DeleteAll(false);
                    Models.Request.DeleteAll(false);
                    IsDialogOpen = false;
                    Open();
                })
            }, "InnerDialog");
            
            Open();

        }, d => !IsDialogOpen && Items.Count > 0));

        private static Requests _instance;
        public static Requests Instance => _instance ?? (_instance = new Requests());

        private ListCollectionView _items;

        public ListCollectionView Items
        {
            get
            {
                if (_items != null) return _items;
                _items = new ListCollectionView(Models.Request.Cache);
                _items.Filter = FilterRequest;
                Models.Request.Cache.CollectionChanged += (sender, args) =>
                {
                    _items.Filter = FilterRequest;
                };
                return _items;
            }
        }

        private bool FilterRequest(object o)
        {
            if (!(o is Request req)) return false;
            if (!req.Submitted) return false;

            if (!ShowProcessed && req.Status > Request.Statuses.Pending) return false;
            
            return true;
        }

        private bool _ShowProcessed = true;

        public bool ShowProcessed
        {
            get => _ShowProcessed;
            set
            {
                if(value == _ShowProcessed)
                    return;
                _ShowProcessed = value;
                OnPropertyChanged(nameof(ShowProcessed));
            }
        }
        
    }
}
