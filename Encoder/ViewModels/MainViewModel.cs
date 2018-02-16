using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NetworkCommsDotNet;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe.ViewModels
{
    sealed class MainViewModel : ViewModelBase
    {
        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());

        private Encoder _Encoder;

        public Encoder Encoder
        {
            get => _Encoder;
            set
            {
                if (value == _Encoder) return;
                _Encoder = value;
                OnPropertyChanged(nameof(Encoder));
            }
        }

        private MainViewModel()
        {
            NetworkComms.AppendGlobalIncomingPacketHandler<ServerUpdate>(ServerUpdate.GetHeader(), (h, c, u) =>
            {
                PendingRequestCount = u.Requests;
                OnlineEncoderCount = u.Encoders;
            });
        }

        private int? _OnlineEncoderCount;

        public int? OnlineEncoderCount
        {
            get => _OnlineEncoderCount;
            set
            {
                if (value == _OnlineEncoderCount) return;
                _OnlineEncoderCount = value;
                if (value == 0) _OnlineEncoderCount = null;
                OnPropertyChanged(nameof(OnlineEncoderCount));
            }
        }

        

        private int? _PendingRequestCount;

        public int? PendingRequestCount
        {
            get => _PendingRequestCount;
            set
            {
                if (value == _PendingRequestCount) return;
                _PendingRequestCount = value;
                if (value == 0) _PendingRequestCount = null;
                OnPropertyChanged(nameof(PendingRequestCount));
            }
        }
        
        private ICommand _getNextRequest;

        public ICommand GetNextRequest => _getNextRequest ?? (_getNextRequest = new DelegateCommand<Requests>(async win =>
        {
            if (IsProcessing) return;
          //  IsProcessing = true;
          //  await TaskEx.Delay(2000);
          //  IsProcessing = false;
          
            return;
            if (Encoder == null)
            {
                var result = await LoginViewModel.ShowDialog(new LoginView());
                Encoder = result.Result == ResultCodes.Success ? result.Encoder : null;
            }
            
            if (Encoder != null)
            {
                IsProcessing = true;
                var res = await Client.GetNextWork(Encoder.Username);
                IsProcessing = false;
                if (res.Result == ResultCodes.Success)
                {
                    await WorkViewModel.ShowDialog(res);
                }
                else
                    MessageBox.Show("Something went wrong while trying to fetch the next work item.",
                        "Request Timeout", MessageBoxButton.OK, MessageBoxImage.Error);
                
            }
        }));

        private bool _IsProcessing;

        public bool IsProcessing
        {
            get => _IsProcessing;
            set
            {
                if (value == _IsProcessing) return;
                _IsProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        

        private static List<Screen> _screens;

        public List<Screen> Screens
        {
            get
            {
                if (_screens != null) return _screens;
                _screens = new List<Screen>()
                {
                    new Screen("asdf", PackIconKind.AccessPoint),
                    new Screen("asdf", PackIconKind.AccessPoint),
                    new Screen("asdf", PackIconKind.AccessPoint),
                    new Screen("asdf", PackIconKind.AccessPoint)
                };
                var view = CollectionViewSource.GetDefaultView(_screens);
                view.CurrentChanged += (s, a) =>
                {
                    IsMenuOpen = false;
                };
                return _screens;
            }
        }
        
        private bool _IsMenuOpen;

        public bool IsMenuOpen
        {
            get => _IsMenuOpen;
            set
            {
                if (value == _IsMenuOpen) return;
                _IsMenuOpen = value;
                OnPropertyChanged(nameof(IsMenuOpen));
            }
        }

        
    }
}
