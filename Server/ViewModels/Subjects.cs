using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NORSU.EncodeMe;
using Models = NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.ViewModels
{
    class Subjects : Screen
    {
        private Subjects() : base("Offered Courses", PackIconKind.BookOpen)
        {
            ShortName = "Courses";
            Commands.Add(new ScreenMenu("COURSES", PackIconKind.Library, ShowCoursesCommand));
            
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            
            Messenger.Default.AddListener<Models.Course>(Messages.ModelSelected, m =>
            {
                if (FilterByCourse)
                {
                    Items.Filter = FilterSubjects;
                }
            });
        }


        private bool _FilterByCourse;

        public bool FilterByCourse
        {
            get => _FilterByCourse;
            set
            {
                if (value == _FilterByCourse) return;
                _FilterByCourse = value;
                OnPropertyChanged(nameof(FilterByCourse));
                Items.Filter = FilterSubjects;
                if (!value && Items.CurrentItem == null)
                    Items.MoveCurrentToFirst();
            }
        }

        private bool FilterSubjects(object o)
        {
            if (!FilterByCourse) return true;
            var subject = o as Models.Subject;
            if (subject == null) return false;
            
            var cs =  Models.CourseSubject.Cache.FirstOrDefault(x => x.SubjectId == subject.Id);
            if (cs == null) return false;
            var course = Models.Course.Cache.FirstOrDefault(x => x.Id == cs.CourseId);
            return course?.IsSelected ?? false;
        }

        private static Subjects _instance;

        public static Subjects Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new Subjects();

                return _instance;
            }
        }

        private static ICommand _deleteSubjectsCommand;

        public ICommand ClearSubjectsCommand =>
            _deleteSubjectsCommand ?? (_deleteSubjectsCommand = new DelegateCommand(
                d =>
                {
                    DialogHost.Show(new MessageDialog()
                    {
                        Icon = PackIconKind.DeleteVariant,
                        Message = "Are you sure you want to delete all subjects?\nThis action cannot be undone.",
                        Title = "CONFIRM DELETE",
                        Affirmative = "DELETE ALL",
                        Negative = "CANCEL",
                        AffirmativeAction = ()=> Models.Subject.DeleteAll()
                    }, "InnerDialog");
                }, d=>Models.Subject.Cache.Count>0));

        private static ICommand _clearSchedulesCommand;

        public ICommand ClearSchedulesCommand =>
            _clearSchedulesCommand ?? (_clearSchedulesCommand = new DelegateCommand(
                d =>
                {
                    var subject = (Models.Subject) Items.CurrentItem;
                    DialogHost.Show(new MessageDialog()
                    {
                        Icon = PackIconKind.DeleteVariant,
                        Message = $"Are you sure you want to delete all class schedules for {subject.Code}?\nThis action cannot be undone.",
                        Title = "CONFIRM DELETE",
                        Affirmative = "DELETE ALL",
                        Negative = "CANCEL",
                        AffirmativeAction = () =>
                        {
                            Models.ClassSchedule.DeleteWhere(nameof(Models.ClassSchedule.SubjectId), subject.Id);
                            IsDialogOpen = false;
                        }
                    }, "InnerDialog");
                }, d =>
                {
                    if (!(Items.CurrentItem is Models.Subject subject)) return false;
                    return Models.ClassSchedule.Cache.Count(x=>x.SubjectId==subject.Id) > 0;
                }));

        private static ICommand _showCoursesCommand;
        
        public ICommand ShowCoursesCommand => _showCoursesCommand ?? (_showCoursesCommand = new DelegateCommand(d =>
        {
            IsRightDrawerOpen = true;
            RightDrawerContent = Courses.Instance;
        }));

        private static ICommand _showSchedulesCommand;
        private static readonly Schedules _schedules = new Schedules();

        public ICommand ShowSchedulesCommand => _showSchedulesCommand ?? (_showSchedulesCommand = new DelegateCommand(
        d =>
        {
            IsRightDrawerOpen = true;
            RightDrawerContent = _schedules;
        }));
        
        private bool _AllSubjectsSelected;

        public bool AllSubjectsSelected
        {
            get => Models.Subject.Cache.All(x => x.IsSelected);
            set
            {
                foreach (var course in Items)
                {
                    if (course.GetType() != typeof(Models.Subject)) continue;
                    if (course is Models.Subject c)
                        c.IsSelected = value;
                }
                OnPropertyChanged(nameof(AllSubjectsSelected));
            }
        }
        
        private ListCollectionView _items;

        public ListCollectionView Items
        {
            get
            {
                if (_items != null) return _items;
                _items = new ListCollectionView(Models.Subject.Cache);
                _items.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtBeginning;
                _items.CurrentChanged += (sender, args) =>
                {
                    Models.ClassSchedule.SetNewScheduleSubject(((Models.Subject) _items.CurrentItem).Id);
                    Schedules.Filter = FilterSchedule;
                };
                return _items;
            }
        }

        public override void Open()
        {
            if (Models.Course.Cache.Count == 0)
            {
                ShowCoursesCommand.Execute(null);
            }
        }

        private ListCollectionView _schedulesView;
        public ListCollectionView Schedules
        {
            get
            {
                if (_schedulesView != null) return _schedulesView;
                _schedulesView = new ListCollectionView(Models.ClassSchedule.Cache);
                _schedulesView.Filter = FilterSchedule;
                return _schedulesView;
            }
        }

        private bool FilterSchedule(object o)
        {
            if (!(Items.CurrentItem is Models.Subject)) return false;
            var sched = (Models.ClassSchedule) o;
            return sched.SubjectId == ((Models.Subject) Items.CurrentItem)?.Id;
        }
    }
}
