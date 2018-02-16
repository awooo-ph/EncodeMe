using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using NORSU.EncodeMe;
using NORSU.EncodeMe.Properties;
using Models = NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class Encoders : Screen
    {
        private Encoders() : base("Enrollment Encoders", PackIconKind.Keyboard)
        {
            ShortName = "Encoders";
            Commands.Add(new ScreenMenu("ADD NEW", PackIconKind.AccountPlus, AddNewCommand));
            Commands.Add(new ScreenMenu("DELETE ALL", PackIconKind.AccountMultipleMinus, DeleteAllCommand));

            Messenger.Default.AddListener<Models.Encoder>(Messages.EncoderFlipped, encoder =>
            {
                if(_previousFlipped!=null)
                     _previousFlipped.IsFlipped = false;
                _previousFlipped = encoder;
            });

            Messenger.Default.AddListener<Models.Encoder>(Messages.ModelDeleted, EncoderDeleted);
            Messenger.Default.AddListener<Models.Encoder>(Messages.ChangeEncoderPicture, ChangeEncoderPicture);
            Messenger.Default.AddListener<Models.Encoder>(Messages.ChangeEncoderPassword, ChangeEncoderPassword);
        }

        private static Encoders _instance;
        public static Encoders Instance => _instance ?? (_instance = new Encoders());

        public override async void Open()
        {
            if (Models.Encoder.Cache.Count == 0)
            {
                await TaskEx.Delay(300);
                
               AddNew();
            }
        }

        public bool HasEncoders => Models.Encoder.Cache.Count > 0;

        private static ICommand _addNewCommand;

        private ICommand AddNewCommand => _addNewCommand ?? (_addNewCommand = new DelegateCommand(d =>AddNew(), d => !IsDialogOpen));

        private async void AddNew()
        {
            var newUser = new NewUserDialog();
            newUser.AcceptCommand = new DelegateCommand(dd =>
            {
                IsDialogOpen = false;
                var encoder = new Models.Encoder();
                encoder.Username = newUser.Username;
                encoder.FullName = newUser.FullName;
                encoder.Save();
            }, dd => !string.IsNullOrWhiteSpace(newUser.Username) && !Models.Encoder.UsernameExists(newUser.Username));

            await DialogHost.Show(new Views.NewUserDialog() {DataContext = newUser}, "InnerDialog");
            
            OnPropertyChanged(nameof(HasEncoders));
        }

        private static ICommand _deleteAllCommand;
        private ICommand DeleteAllCommand => _deleteAllCommand ?? (_deleteAllCommand = new DelegateCommand(async d =>
        {
            await DialogHost.Show(new MessageDialog()
            {
                Icon = PackIconKind.DeleteForever,
                Title = "CONFIRM DELETE",
                Message = "Are you sure you want to delete all encoders?\nThis action cannot be undone.",
                Affirmative = "DELETE ALL",
                Negative = "CANCEL",
                AffirmativeCommand = new DelegateCommand(dd =>
                {
                    Models.Encoder.DeleteAll();
                    IsDialogOpen = false;
                    Open();
                })
            }, "InnerDialog");
            OnPropertyChanged(nameof(HasEncoders));
            Open();

        }, d => !IsDialogOpen && Items.Count > 0));


        private void EncoderDeleted(Models.Encoder encoder)
        {
            Open();
            MainViewModel.EnqueueMessage(
                $"{encoder.FullName} ({encoder.Username}) is successfully deleted.",
                "UNDO", en =>
                {
                    en.Update(nameof(en.IsDeleted), false);
                    Models.Encoder.Cache.Add(en);
                    _items.Refresh();
                    OnPropertyChanged(nameof(HasEncoders));
                }, encoder, true);
            OnPropertyChanged(nameof(HasEncoders));
        }

        private void ChangeEncoderPassword(Models.Encoder encoder)
        {
            var pwd = encoder.Password;
            encoder.Update(nameof(Models.Encoder.Password), "");

            MainViewModel.EnqueueMessage(
                $"{encoder.Username}'s password has been reset.",
                "UNDO", en =>
                {
                    en.Update(nameof(Models.Encoder.Password), pwd);
                }, encoder, true);
        }

        private void ChangeEncoderPicture(Models.Encoder encoder)
        {
            {
                var dlg = new OpenFileDialog();
                dlg.Title = "Select Picture";
                dlg.Multiselect = false;
                dlg.Filter = @"All Images|*.BMP;*.JPG;*.JPEG;*.GIF;*.PNG|
                               BMP Files|*.BMP;*.DIB;*.RLE|
                               JPEG Files|*.JPG;*.JPEG;*.JPE;*.JFIF|
                               GIF Files|*.GIF|
                               PNG Files|*.PNG";

                if (string.IsNullOrWhiteSpace(Settings.Default.OpenFileLocation))
                    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                else
                    dlg.InitialDirectory = Settings.Default.OpenFileLocation;

                if (!dlg.ShowDialog(App.Current.MainWindow) ?? false) return;

                Settings.Default.OpenFileLocation = Path.GetDirectoryName(dlg.FileName);
                Settings.Default.Save();

                try
                {

                    using (var img = Image.FromFile(dlg.FileName))
                    {
                        using (var bmp = ImageProcessor.Resize(img, 471))
                        {
                            using (var bin = new MemoryStream())
                            {
                                bmp.Save(bin, ImageFormat.Jpeg);
                                encoder.Picture = bin.ToArray();
                            }
                        }
                        using (var bmp = ImageProcessor.Resize(img, 74))
                        {
                            using (var bin = new MemoryStream())
                            {
                                bmp.Save(bin, ImageFormat.Jpeg);
                                encoder.Thumbnail = bin.ToArray();
                            }
                        }
                    }
                    var pic = encoder.Picture;

                    encoder.Save();
                    encoder.IsFlipped = false;

                    MainViewModel.EnqueueMessage($"{encoder.Username}'s picture has been changed.", "UNDO",
                        en =>
                        {
                            en.Picture = pic;
                            en.Save();
                        }, encoder, true);


                }
                catch (Exception e)
                {
                    Messenger.Default.Broadcast(Messages.Error, e);
                }

            }
        }

        private Models.Encoder _previousFlipped = null;

        public ListCollectionView _items;

        public ListCollectionView Items
        {
            get
            {

                if (_items != null) return _items;
                _items = (ListCollectionView)CollectionViewSource.GetDefaultView(Models.Encoder.Cache);
                _items.SortDescriptions.Add(new SortDescription(nameof(Models.Encoder.Username), ListSortDirection.Ascending));
                return _items;
            }
        }



    }
}
