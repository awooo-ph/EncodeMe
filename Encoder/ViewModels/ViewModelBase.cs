using System;
using System.ComponentModel;
using System.Threading;
using NORSU.EncodeMe.Annotations;

static class awooo
{
    private static SynchronizationContext _context;

    public static SynchronizationContext Context
    {
        get { return _context; }
        set
        {
            if (_context != null)
                return;
            _context = value;
        }
    }

    public static void Post(Action action)
    {
        Context.Post(d=>action.Invoke(),null);
    }
}

namespace NORSU.EncodeMe.ViewModels
{
    
    abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            awooo.Post(()=>PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
