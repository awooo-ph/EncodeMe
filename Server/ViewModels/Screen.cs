using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace NORSU.EncodeMe.ViewModels
{
    class Screen : ViewModelBase
    {
        public Screen(string name, PackIconKind icon)
        {
            Name = name;
            Icon = icon;
            //   Content = content;
        }

        private List<ScreenMenu> _Commands;
        public List<ScreenMenu> Commands => _Commands ?? (_Commands = new List<ScreenMenu>());

        private bool _IsDialogOpen;

        public bool IsDialogOpen
        {
            get => _IsDialogOpen;
            set
            {
                if (value == _IsDialogOpen) return;
                _IsDialogOpen = value;
                OnPropertyChanged(nameof(IsDialogOpen));
            }
        }
        
        private bool _IsRightDrawerOpen;

        public bool IsRightDrawerOpen
        {
            get => _IsRightDrawerOpen;
            set
            {
                if (value == _IsRightDrawerOpen) return;
                _IsRightDrawerOpen = value;
                OnPropertyChanged(nameof(IsRightDrawerOpen));
            }
        }

        private object _RightDrawerContent;

        public object RightDrawerContent
        {
            get => _RightDrawerContent;
            set
            {
                if (value == _RightDrawerContent) return;
                _RightDrawerContent = value;
                OnPropertyChanged(nameof(RightDrawerContent));
            }
        }

        private string _Badge;

        public string Badge
        {
            get => _Badge;
            set
            {
                if (value == _Badge) return;
                _Badge = value;
                OnPropertyChanged(nameof(Badge));
            }
        }

        private string _ShortName;

        public string ShortName
        {
            get => _ShortName;
            set
            {
                if(value == _ShortName)
                    return;
                _ShortName = value;
                OnPropertyChanged(nameof(ShortName));
            }
        }

        

        private ScrollBarVisibility _HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => _HorizontalScrollBarVisibility;
            set
            {
                if (value == _HorizontalScrollBarVisibility) return;
                _HorizontalScrollBarVisibility = value;
                OnPropertyChanged(nameof(HorizontalScrollBarVisibility));
            }
        }

        private ScrollBarVisibility _VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => _VerticalScrollBarVisibility;
            set
            {
                if (value == _VerticalScrollBarVisibility) return;
                _VerticalScrollBarVisibility = value;
                OnPropertyChanged(nameof(VerticalScrollBarVisibility));
            }
        }

        public virtual void Open() { }


        public virtual void Close()
        {
            IsDialogOpen = false;
            IsRightDrawerOpen = false;
        }

        private Visibility _Visibility = Visibility.Visible;

        public Visibility Visibility
        {
            get => _Visibility;
            set
            {
                if (value == _Visibility) return;
                _Visibility = value;
                OnPropertyChanged(nameof(Visibility));
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private PackIconKind _Icon;

        public PackIconKind Icon
        {
            get => _Icon;
            set
            {
                if (value == _Icon) return;
                _Icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }
        
        //private UserControl _content;

        //public UserControl Content
        //{
        //    get => _content;
        //    set
        //    {
        //        _content = value;
        //        if (value != null)
        //            _content.DataContext = this;
        //        OnPropertyChanged(nameof(Content));
        //    }
        //}
    }
}
