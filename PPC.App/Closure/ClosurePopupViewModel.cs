﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using EasyIoc;
using EasyMVVM;
using PPC.Domain;
using PPC.Module.Notes.ViewModels;
using PPC.Services.Popup;

namespace PPC.App.Closure
{
    public enum ClosureDisplayModes
    {
        Articles,
        CashCount,
        Notes,
    }

    [PopupAssociatedView(typeof(ClosurePopup))]
    public class ClosurePopupViewModel : ObservableObject
    {
        private IPopupService PopupService => IocContainer.Default.Resolve<IPopupService>();

        private readonly Action _closeAction;
        private readonly Func<Domain.Closure, Task> _sendMailsAsyncFunc;

        private bool _isWaiting;
        public bool IsWaiting
        {
            get { return _isWaiting; }
            protected set { Set(() => IsWaiting, ref _isWaiting, value); }
        }

        private ArticlesViewModel _articlesViewModel;
        public ArticlesViewModel ArticlesViewModel
        {
            get { return _articlesViewModel;}
            protected set { Set(() => ArticlesViewModel, ref _articlesViewModel, value); }
        }

        private CashCountViewModel _cashcountViewModel;
        public CashCountViewModel CashCountViewModel
        {
            get { return _cashcountViewModel; }
            protected set { Set(() => CashCountViewModel, ref _cashcountViewModel, value); }
        }

        // Will reuse MainViewModel.NotesViewModel
        public NotesViewModel NotesViewModel { get; protected set; }

        private ClosureDisplayModes _mode;
        public ClosureDisplayModes Mode
        {
            get { return _mode; }
            protected set
            {
                if (Set(() => Mode, ref _mode, value))
                {
                    RaisePropertyChanged(() => IsArticlesSelected);
                    RaisePropertyChanged(() => IsCashCountSelected);
                    RaisePropertyChanged(() => IsNotesSelected);
                }
            }
        }

        public bool IsArticlesSelected => Mode == ClosureDisplayModes.Articles;
        public bool IsCashCountSelected => Mode == ClosureDisplayModes.CashCount;
        public bool IsNotesSelected => Mode == ClosureDisplayModes.Notes;

        private ICommand _okCommand;
        public ICommand OkCommand => _okCommand = _okCommand ?? new RelayCommand(Ok);
        private void Ok()
        {
            PopupService?.Close(this);
            _closeAction?.Invoke();
        }

        private ICommand _sendMailsCommand;
        public ICommand SendMailsCommand => _sendMailsCommand = _sendMailsCommand ?? new RelayCommand(async () => await SendMails());

        private async Task SendMails()
        {
            IsWaiting = true;
            CashRegisterClosure cashRegisterClosure = ArticlesViewModel.ClosureData;
            Domain.Closure closure = new Domain.Closure
            {
                CreationTime = DateTime.Now,
                Notes = NotesViewModel.Note,
                CashRegisterClosure = cashRegisterClosure
            };
            await _sendMailsAsyncFunc(closure);
            IsWaiting = false;
        }

        private ICommand _switchToArticlesCommand;
        public ICommand SwitchToArticlesCommand => _switchToArticlesCommand = _switchToArticlesCommand ?? new RelayCommand(() => Mode = ClosureDisplayModes.Articles);

        private ICommand _switchToCashCountCommand;
        public ICommand SwitchToCashCountCommand => _switchToCashCountCommand = _switchToCashCountCommand ?? new RelayCommand(() => Mode = ClosureDisplayModes.CashCount);

        private ICommand _switchToNotesCommand;
        public ICommand SwitchToNotesCommand => _switchToNotesCommand = _switchToNotesCommand ?? new RelayCommand(SwitchToNotes);

        private void SwitchToNotes()
        {
            Mode = ClosureDisplayModes.Notes;
            NotesViewModel.GotFocus();
        }

        //http://stackoverflow.com/questions/12466049/passing-an-awaitable-anonymous-function-as-a-parameter
        public ClosurePopupViewModel(NotesViewModel notesViewModel, Action closeAction, CashRegisterClosure cashRegisterClosure, Func<Domain.Closure, Task> sendMailsAsyncFunc)
        {
            Mode = ClosureDisplayModes.Articles;

            _closeAction = closeAction;
            _sendMailsAsyncFunc = sendMailsAsyncFunc;

            ArticlesViewModel = new ArticlesViewModel(cashRegisterClosure);
            CashCountViewModel = new CashCountViewModel();
            NotesViewModel = notesViewModel;
        }
    }

    public class ClosurePopupViewModelDesignData : ClosurePopupViewModel
    {
        public ClosurePopupViewModelDesignData() : base(null, () => { }, new CashRegisterClosure(), null)
        {
            ArticlesViewModel = new ArticlesViewModelDesignData();
            CashCountViewModel = new CashCountViewModelDesignData();
            NotesViewModel = new NotesViewModelDesignData();
        }
    }
}
