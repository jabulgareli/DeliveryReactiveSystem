﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using Drs.Infrastructure.Time;
using Drs.Model.Account;
using Drs.Model.Settings;
using Drs.Model.UiView.Shared;
using Drs.ViewModel.Account;
using Drs.ViewModel.External;
using Drs.ViewModel.Menu;
using Drs.ViewModel.Order;
using Drs.ViewModel.Shared;
using Drs.ViewModel.Status;
using Drs.ViewModel.Track;
using MahApps.Metro.Controls;
using ReactiveUI;

namespace Drs.ViewModel.Main
{
    public class ShellContainerVm : ReactiveObject, IShellContainerVm
    {
        private IUcViewModel _currentView;
        private IUcViewModel _lastView;

        public IUcViewModel CurrentView
        {
            get { return _currentView; }
            set
            {
                this.RaiseAndSetIfChanged(ref _currentView, value);
            }
        }

        //public ICurrentUserSettings CurrentUserSettings { get; set; }

        public ReadOnlyDictionary<StatusScreen, IUcViewModel> DictionaryViews { get; set; }
        public void ChangeCurrentView(StatusScreen statusScreen, bool bHasToInit, bool bForzeToInit = false, string parameters = null)
        {
            if (_lastView != null)
            {
                CurrentView = _lastView;
                _lastView = null;
            }
            else
            {
                CurrentView = DictionaryViews[statusScreen];
            }

            HasToShowHeader(statusScreen);

            if (bHasToInit)
                CurrentView.Initialize(bForzeToInit, parameters);
        }

        private void HasToShowHeader(StatusScreen statusScreen)
        {
            switch (statusScreen)
            {
                case StatusScreen.Login:
                case StatusScreen.UmEx:
                case StatusScreen.ShMenu:
                    HeaderVisibility = Visibility.Collapsed;
                    CloseFlyouts();
                    return;
            }

            HeaderVisibility = Visibility.Visible;
        }

        private void CloseFlyouts()
        {
            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                foreach (var flyout in Flyouts.Where(flyout => flyout != null && flyout.IsOpen))
                {
                    flyout.IsOpen = false;
                }
            });
        }

        private Visibility _headerVisibility;
        private IReactiveList<Flyout> _flyouts;
        private bool _isInOrder;
        private string _userTitle;

        public Visibility HeaderVisibility
        {
            get
            {
                return _headerVisibility;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _headerVisibility, value);
            }
        }

        public IReactiveList<Flyout> Flyouts
        {
            get { return _flyouts; }
            private set
            {
                this.RaiseAndSetIfChanged(ref _flyouts, value);
            }
        }

        public BootstrapperBase BootStrapper { get; set; }

        public bool IsInOrder
        {
            get { return _isInOrder; }
            set
            {
                this.RaiseAndSetIfChanged(ref _isInOrder, value);
            }
        }

        public void AddOrUpdateFlyouts(IFlyoutBaseVm flyout)
        {
            var flyoutType = flyout.GetType();
            var oldFlyout = Flyouts.FirstOrDefault(e => e.GetType().Name == flyoutType.Name);
            if (oldFlyout != null)
                Flyouts.Remove(oldFlyout);
            Flyouts.Add(BootStrapper.CreateFlyoutControl(flyout));
        }

        public ShellContainerVm(ILoginVm loginVm, IMenuVm menuVm, IMainOrderVm orderVm, IDailyOrderStatusVm dailyOrderVm, ITrackOrderVm trackOrderVm
            , IMsgWndVm msgWndVm, IBrowserVm browserVm)
        {
            HeaderVisibility = Visibility.Collapsed;

            loginVm.UserChanged += OnUserChanged;

            Flyouts = new ReactiveList<Flyout>();

            DictionaryViews = new ReadOnlyDictionary<StatusScreen, IUcViewModel>(new Dictionary<StatusScreen, IUcViewModel>
            {
                {StatusScreen.Login, loginVm},
                {StatusScreen.ShMenu, menuVm},
                {StatusScreen.UmOrd, orderVm},
                {StatusScreen.UmTrc, trackOrderVm},
                {StatusScreen.UmMsg, msgWndVm},
                {StatusScreen.UmStt, dailyOrderVm},
                {StatusScreen.UmEx, browserVm}
            });

            CurrentView = DictionaryViews[StatusScreen.UmMsg];
            SetIdleWindow();
        }

        public void OnUserChanged(UserInfoModel user)
        {
            UserTitle = String.Format("Bienvenido:  {0} ({1}) | {2}", user.FullName, user.Username, user.Role);
        }

        public string UserTitle
        {
            get { return _userTitle; }
            set { this.RaiseAndSetIfChanged(ref _userTitle, value); }
        }

        public void Initialize()
        {
            IsInOrder = false;
            foreach (var view in DictionaryViews)
                view.Value.ShellContainerVm = this;
        }

        private void SetIdleWindow()
        {
            Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(5))
            .Subscribe(x =>
            {
                var idle = IdleTimeDetector.GetIdleTimeInfo();
                if (idle.IdleTime.TotalSeconds > SettingsData.Client.TotalSecondsToLogOut)
                {
                    if (DictionaryViews != null && CurrentView != DictionaryViews[StatusScreen.Login])
                    {
                        ChangeCurrentView(StatusScreen.Login, false);
                    }
                }
            });
        }
    }
}
