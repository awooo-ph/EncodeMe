using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe.Annotations;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        private static MainViewModel _instance;
        public static MainViewModel Instance => _instance ?? (_instance = new MainViewModel());

        private MainViewModel()
        {     
            Messenger.Default.AddListener<ClassSchedule>(Messages.ModelDeleted, s =>
            {
                EnqueueMessage("Class schedule has been deleted.","UNDO",
                    sc =>
                    {
                        sc.Update(nameof(sc.IsDeleted),false);
                        ClassSchedule.Cache.Add(sc);
                    },s,true);
            });

            Messenger.Default.AddListener<Subject>(Messages.ModelDeleted, s =>
            {
                EnqueueMessage($"{s.Code} has been deleted.", "UNDO",
                    sc =>
                    {
                        sc.Update(nameof(sc.IsDeleted), false);
                        Subject.Cache.Add(sc);
                    }, s, true);
            });
        }

        private User _CurrentUser;

        public User CurrentUser
        {
            get => _CurrentUser;
            set
            {
                if(value == _CurrentUser)
                    return;
                _CurrentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(HasLoggedIn));
            }
        }

        private string _LoginUsername;

        public string LoginUsername
        {
            get => _LoginUsername;
            set
            {
                if(value == _LoginUsername)
                    return;
                _LoginUsername = value;
                OnPropertyChanged(nameof(LoginUsername));
            }
        }

        private ICommand _logoutCommand;

        public ICommand LogoutCommand => _logoutCommand ?? (_logoutCommand = new DelegateCommand(d =>
        {
            Messenger.Default.Broadcast(Messages.InitiateLogout);
        }));

        private ICommand _loginCommand;
        public ICommand LoginCommand => _loginCommand ?? (_loginCommand = new DelegateCommand<PasswordBox>(
        async pwd =>
        {
            User user = null;
            if (User.Cache.Count == 0)
            {
                user = new User()
                {
                    Username = LoginUsername,
                    Password = pwd.Password,
                    Picture = ImageProcessor.GetRandomLego()
                };
            } else
                user = User.Cache.FirstOrDefault(x => x.Username.ToLower() == LoginUsername.ToLower());

            if (user == null)
            {
                await DialogHost.Show(new MessageDialog()
                {
                    Icon = PackIconKind.KeyVariant,
                    Title = "AUTHENTICATION FAILED",
                    Message = "Invalid username and password. Please try again!",
                    Affirmative = "QUIT PROGRAM",
                    Negative = "TRY AGAIN",
                    AffirmativeCommand = new DelegateCommand(dd =>
                    {
                        Application.Current.Shutdown();
                    })
                }, "InnerDialog");
                return;
            }
            if (string.IsNullOrEmpty(user.Password))
                user.Password = pwd.Password;

            if (user.Password != pwd.Password)
            {
                await DialogHost.Show(new MessageDialog()
                {
                    Icon = PackIconKind.KeyVariant,
                    Title = "AUTHENTICATION FAILED",
                    Message = "Invalid username and password. Please try again!",
                    Affirmative = "QUIT PROGRAM",
                    Negative = "TRY AGAIN",
                    AffirmativeCommand = new DelegateCommand(dd =>
                    {
                        Application.Current.Shutdown();
                    })
                }, "InnerDialog");
                return;
            }
            
            user.Save();
            LoginUsername = "";
            pwd.Password = "";
            Messenger.Default.Broadcast(Messages.InitiateLogin,user);
        },pwd=>pwd?.Password.Length>0 && !string.IsNullOrWhiteSpace(LoginUsername)));

        public bool HasLoggedIn => CurrentUser != null;

        private ObservableCollection<Screen> _items;
        private ListCollectionView _itemsView;

        public ObservableCollection<Screen> Screens
        {
            get
            {
                if (_itemsView != null) return _items;//View;
                _items = new ObservableCollection<Screen>()
                {
                    //new Screen("Dashboard", PackIconKind.Home),
                    Students.Instance,
                    Encoders.Instance,
                    Subjects.Instance,
                    Requests.Instance,
                    Terminals.Instance,
                    Users.Instance,
                 //   new Screen("Activity", PackIconKind.Clock),
                 //   new Screen("Settings", PackIconKind.Settings),
                };
                _itemsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_items);
                _itemsView.CurrentChanging += (s, args) =>
                {
                    var cur = ((Screen)_itemsView.CurrentItem);
                    cur?.Close();
                };
                _itemsView.CurrentChanged += (sender, args) =>
                {
                    (_itemsView.CurrentItem as Screen)?.Open();
                    Instance.IsLeftDrawerOpen = false;
                    OnPropertyChanged(nameof(CurrentScreen));
                };
                return _items;//View;
            }
        }

        public Screen CurrentScreen => (Screen) CollectionViewSource.GetDefaultView(Screens).CurrentItem;

        public static void EnqueueMessage(string message, bool promote = false)
        {
            Instance.MessageQueue.Enqueue(message, promote);
        }

        public static void EnqueueMessage<T>(string message, string action, Action<T> actionHandler, T param, bool promote = false)
        {
            Instance.MessageQueue.Enqueue(message, action, actionHandler, param, promote);
        }
        public static void EnqueueMessage(string message, string action, Action actionHandler, bool promote = false)
        {
            Instance.MessageQueue.Enqueue(message, action, actionHandler, promote);
        }

        private SnackbarMessageQueue _messageQueue;

        public SnackbarMessageQueue MessageQueue =>
            _messageQueue ?? (_messageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(7)));

        private bool _isLeftDrawerOpen;

        public bool IsLeftDrawerOpen
        {
            get => _isLeftDrawerOpen;
            set
            {
                _isLeftDrawerOpen = value;
                OnPropertyChanged(nameof(IsLeftDrawerOpen));
            }
        }

    }
}
