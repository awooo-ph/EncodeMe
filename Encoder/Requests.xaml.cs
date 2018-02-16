
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GenieLib;
using MaterialDesignThemes.Wpf;
using NetworkCommsDotNet.Tools;
using NORSU.EncodeMe.Network;
using NORSU.EncodeMe.ViewModels;

namespace NORSU.EncodeMe
{
    /// <summary>
    /// Interaction logic for Requests.xaml
    /// </summary>
    public partial class Requests : Window
    {
       // private Magic _loginMagic, _encoderMagic, _workMagic;
        
        public Requests()
        {
            InitializeComponent();

          //  _loginMagic = new Magic(LoginLamp, Genie, false);
          //  _loginMagic.Duration = new Duration(TimeSpan.FromMilliseconds(200));
          //  _loginMagic.Collapsed += MagicCollapsed;

          //  _loginMagic.Expanding += GenieExpanding;

          //  _loginMagic.Expanded += (sender, args) =>
          //  {
          //      MainTransitioner.SelectedIndex = 1;
         //   };
            
          //  _loginMagic.IsGenieOut = false;


          //  _encoderMagic = new Magic(LoginLamp, EncoderGenie, true);
         //   _encoderMagic.Duration = new Duration(TimeSpan.FromMilliseconds(200));
          //  _encoderMagic.Expanding += GenieExpanding;
          //  _encoderMagic.Collapsed += MagicCollapsed;

          //  _workMagic = new Magic(NextButton, WorkGenie, true);
          //  _workMagic.Duration = new Duration(TimeSpan.FromMilliseconds(200));
          //  _workMagic.Collapsed += MagicCollapsed;
          //  _workMagic.Expanding += GenieExpanding;
        }

        private void MagicCollapsed(object o, EventArgs eventArgs)
        {
            ((Magic)o).Genie.Visibility = Visibility.Hidden;
            MainTransitioner.SelectedIndex = 0;
        }

        private void GenieExpanding(object o, EventArgs eventArgs)
        {
            ((Magic)o).Genie.Visibility = Visibility.Visible;
        }

        private void Requests_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
            //    DragMove();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Left = System.Windows.SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Bottom - ActualHeight;
        }

        private GetWorkResult CurrentWork;
        private bool workFetched;
        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (CurrentWork?.Result == ResultCodes.Success)
            {
                MainTransitioner.SelectedIndex = 3;
                Content.SelectedIndex = 2;
                return;
            }
            
            var NextButton = (Button) sender;
            if (MainViewModel.Instance.Encoder == null)
            {     
                MainTransitioner.SelectedIndex = 1;
                PeerDiscovery.DiscoverPeersAsync(PeerDiscovery.DiscoveryMethod.UDPBroadcast);
                return;
            }
            
            if (workFetched) return;
            workFetched = true;
            ButtonProgressAssist.SetIsIndicatorVisible(NextButton, true);
            ButtonProgressAssist.SetIsIndeterminate(NextButton, true);
            var work = await Client.GetNextWork();
            CurrentWork = work;
            ButtonProgressAssist.SetIsIndicatorVisible(NextButton, false);
            ButtonProgressAssist.SetIsIndeterminate(NextButton, false);

            //_encoderMagic.IsGenieOut = false;
            var errorMessage = "";
            switch (work.Result)
            {
                case ResultCodes.Success:
                    WorkDataGrid.ItemsSource = work.ClassSchedules;
                    //_workMagic.IsGenieOut = true;
                    MainTransitioner.SelectedIndex = 3;
                    LoginLamp.Visibility = Visibility.Collapsed;
                    StudentId.Text = work.StudentId;
                    StudentName.Text = work.StudentName;
                    break;
                case ResultCodes.NotFound:
                    //MessageBox.Show("No more pending items.");
                    errorMessage = "No more requests";
                    break;
                case ResultCodes.Offline:
                    errorMessage = "Can not find server";
                    break;
                case ResultCodes.Timeout:
                    errorMessage = "Request timeout";
                    break;
                case ResultCodes.Error:
                    errorMessage = "Request timeout";
                    break;
            }

            
            await ShowMessage(errorMessage);
            
            
            workFetched = false;           
        }


        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            //_loginMagic.IsGenieOut = false;
            MainTransitioner.SelectedIndex = 0;
            EnableLogin();
            Username.Text = "";
            Password.Password = "";
        }

        private void EncoderPictureClicked(object sender, MouseButtonEventArgs e)
        {
            if (MainViewModel.Instance.Encoder == null)
            {
                MainTransitioner.SelectedIndex = 0;
                Content.SelectedIndex = 0;
                EnableLogin();
            }
            else
            {
                MainTransitioner.SelectedIndex = 1;
                Content.SelectedIndex = 2;
            }
        }

        private void EnableLogin()
        {
            var enabled = !LoginProgress.IsIndeterminate;
            Username.IsEnabled = enabled;
            Password.IsEnabled = enabled;
            CalculateLoginProgress();
        }
        
        private double loginProgress = 0;
        private async void LoginClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username.Text)) return;
            if (Password.Password.Length==0) return;
            LoginProgress.IsIndeterminate = true;
            EnableLogin();

            var result = await Client.Login(Username.Text, Password.Password);
            if (result.Result == ResultCodes.Success)
            {
                MainViewModel.Instance.Encoder = result.Encoder;
                MainTransitioner.SelectedIndex = 2;
                Content.SelectedIndex = 1;
                Username.Text = "";
                Password.Password = "";
            }
            else
            {
               await ShowMessage("Login Failed");
            }

            LoginProgress.IsIndeterminate = false;
            EnableLogin();
        }

        private async Task ShowMessage(string message)
        {
            if (message == "") return;
            
            Message.Text = message;
            var index = MainTransitioner.SelectedIndex;
            MainTransitioner.SelectedIndex = 4;
            await TaskEx.Delay(2222);
            MainTransitioner.SelectedIndex = index;
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            CalculateLoginProgress();
            
            var login = DataContext as LoginViewModel;
            if (login == null) return;
            login.Password = ((PasswordBox) sender).Password;
        }

        private void Username_OnTextChanged(object sender, TextChangedEventArgs e)
        {
           CalculateLoginProgress();
        }

        private void CalculateLoginProgress()
        {
            var usernameProgress = 0.0;
            if (Username.Text.Length >= 4)
                usernameProgress = 50;
            else
                usernameProgress = (Username.Text.Length / 7.0) * 50;

            var pwdProgress = 0.0;
            if (Password.Password.Length >= 4)
                pwdProgress = 50;
            else
                pwdProgress = (Password.Password.Length / 7.0) * 50;

            LoginProgress.Value = (usernameProgress + pwdProgress);

            LoginButton.IsEnabled = !LoginProgress.IsIndeterminate && LoginProgress.Value == 100;
        }

        private void LogoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            Client.Logout();
            MainViewModel.Instance.Encoder = null;
          //  _encoderMagic.IsGenieOut = false;
            MainTransitioner.SelectedIndex = 1;
            Content.SelectedIndex = 0;
        }

        private void AcceptSelected(object sender, RoutedEventArgs e)
        {
            var selection = CurrentWork.ClassSchedules?.Where(x => x.IsSelected);
            if (selection == null) return;
            foreach (var c in selection)
            {
                c.EnrollmentStatus = ScheduleStatuses.Accepted;
            }
        }

        private void CloseScheduleClicked(object sender, RoutedEventArgs e)
        {
            var selection = CurrentWork.ClassSchedules?.Where(x => x.IsSelected);
            if (selection == null) return;
            foreach (var c in selection)
            {
                c.EnrollmentStatus = ScheduleStatuses.Closed;
            }
        }

        private void ConflictScheduleClicked(object sender, RoutedEventArgs e)
        {
            var selection = CurrentWork.ClassSchedules?.Where(x => x.IsSelected);
            if (selection == null) return;
            foreach (var c in selection)
            {
                c.EnrollmentStatus = ScheduleStatuses.Conflict;
            }
        }

        private static PackIcon submitIcon;

        private async void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            SubmitProgress.Visibility = Visibility.Visible;
            WorkDataGrid.IsEnabled = false;
            WorkToolbar.IsEnabled = false;
            
            var work = new SaveWork()
            {
                StudentId = CurrentWork.StudentId,
                ClassSchedules = CurrentWork.ClassSchedules,
            };

            var result = await Client.SaveWork(work);

            SubmitProgress.Visibility = Visibility.Collapsed;
            WorkDataGrid.IsEnabled = true;
            WorkToolbar.IsEnabled = true;

            if (result.Result == ResultCodes.Success)
            {
               // _workMagic.IsGenieOut = false;
                MainTransitioner.SelectedIndex = 0;
                LoginLamp.Visibility = Visibility.Visible;
            } else if (result.Result == ResultCodes.Denied)
            {
                EncoderPictureClicked(null, null);
            }
            else
            {
                MessageBox.Show("An error occured while contacting server. Please try again.");
            }
        }

        private void Enroll_Click(object sender, RoutedEventArgs e)
        {
            MainTransitioner.SelectedIndex = 5;
            Content.SelectedIndex = 4;
        }

        private void CancelEnrollment_Click(object sender, RoutedEventArgs e)
        {
            MainTransitioner.SelectedIndex = 2;
            Content.SelectedIndex = 1;
        }
    }
}
