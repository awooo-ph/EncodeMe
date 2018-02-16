using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class Users : Screen
    {
        private Users() : base("User Accounts", PackIconKind.AccountMultiple)
        {
            ShortName = "Users";
            Commands.Add(new ScreenMenu("ADD NEW", PackIconKind.AccountPlus, AddNewCommand));
            Commands.Add(new ScreenMenu("DELETE ALL", PackIconKind.AccountMultipleMinus, DeleteAllCommand));
        }

        private static Users _instance;
        public static Users Instance => _instance ?? (_instance = new Users());

        private ListCollectionView _items;

        public ListCollectionView Items
        {
            get
            {
                if (_items != null) return _items;
                _items = new ListCollectionView(Models.User.Cache);
                return _items;
            }
        }

        private static ICommand _deleteAllCommand;

        private ICommand DeleteAllCommand => _deleteAllCommand ?? (_deleteAllCommand = new DelegateCommand(async d =>
        {
            await DialogHost.Show(new MessageDialog()
            {
                Icon = PackIconKind.DeleteForever,
                Title = "CONFIRM DELETE",
                Message = "Are you sure you want to delete all users?\nThis action cannot be undone.",
                Affirmative = "DELETE ALL",
                Negative = "CANCEL",
                AffirmativeCommand = new DelegateCommand(dd =>
                {
                    Models.Encoder.DeleteAll();
                    IsDialogOpen = false;
                    Open();
                })
            }, "InnerDialog");
            OnPropertyChanged(nameof(HasUsers));
            Open();

        }, d => !IsDialogOpen && Items.Count > 0));

        public bool HasUsers => Models.User.Cache.Count > 0;

        private static ICommand _addNewCommand;

        private ICommand AddNewCommand =>
            _addNewCommand ?? (_addNewCommand = new DelegateCommand(d => AddNew(), d => !IsDialogOpen));

        private async void AddNew()
        {
            var newUser = new NewUserDialog("NEW USER");
            newUser.AcceptCommand = new DelegateCommand(dd =>
            {
                IsDialogOpen = false;
                var encoder = new Models.User();
                encoder.Username = newUser.Username;
                encoder.Fullname = newUser.FullName;
                encoder.Picture = ImageProcessor.GetRandomLego();
                encoder.Save();
            }, dd => !string.IsNullOrWhiteSpace(newUser.Username) && User.Cache.All(x => x.Username.ToLower() != newUser.Username.ToLower()));

            await DialogHost.Show(new Views.NewUserDialog() {DataContext = newUser}, "InnerDialog");

            OnPropertyChanged(nameof(HasUsers));
        }
    }
}
