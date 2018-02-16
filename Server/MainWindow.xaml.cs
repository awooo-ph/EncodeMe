using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using NORSU.EncodeMe.Models;
using NORSU.EncodeMe.Network;
using NORSU.EncodeMe.ViewModels;

namespace NORSU.EncodeMe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 20 });
            
            awooo.Context = SynchronizationContext.Current;
            
            Messenger.Default.AddListener(Messages.InitiateLogout, () =>
            {
                awooo.Context.Post(d =>
                {
                    InitiateLogout();
                },null);
            });
            
            Messenger.Default.AddListener<User>(Messages.InitiateLogin, user =>
            {
                awooo.Context.Post(d =>
                {
                    InitiateLogin(user);
                }, null);
            });
        }

        private void InitiateLogout()
        {
            var effect = new BlurEffect();
            effect.RenderingBias = RenderingBias.Performance;
            effect.KernelType = KernelType.Box;
            effect.Radius = 0;
            var duration = new Duration(TimeSpan.FromMilliseconds(333));
            var anim = new DoubleAnimation(0.0, 20.0, duration);
            anim.Completed += (sender, args) =>
            {
                MainViewModel.Instance.CurrentUser = null;
                anim = new DoubleAnimation(0.0,duration);
                anim.Completed += (o, eventArgs) =>
                {
                    Effect = null;
                };
                effect.BeginAnimation(BlurEffect.RadiusProperty, anim);
            };
            Effect = effect;
            effect.BeginAnimation(BlurEffect.RadiusProperty,anim);
        }

        private void InitiateLogin(User user)
        {
            var effect = new BlurEffect();
            effect.RenderingBias = RenderingBias.Performance;
            effect.KernelType = KernelType.Box;
            effect.Radius = 0;
            var duration = new Duration(TimeSpan.FromMilliseconds(333));
            var anim = new DoubleAnimation(0.0, 20.0, duration);
            anim.Completed += (sender, args) =>
            {
                MainViewModel.Instance.CurrentUser = user;
                anim = new DoubleAnimation(0.0, duration);
                anim.Completed += (o, eventArgs) =>
                {
                    Effect = null;
                };
                effect.BeginAnimation(BlurEffect.RadiusProperty, anim);
            };
            Effect = effect;
            effect.BeginAnimation(BlurEffect.RadiusProperty, anim);
        }
    }
}
