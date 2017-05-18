﻿using System;
using System.Windows.Input;
using EasyMVVM;
using PPC.Module.Shop.Views.Popups;
using PPC.Services.Popup;

namespace PPC.Module.Shop.ViewModels.Popups
{
    [PopupAssociatedView(typeof(AskNamePopup))]
    public class AskNamePopupViewModel : ObservableObject
    {
        private IPopupService PopupService => EasyIoc.IocContainer.Default.Resolve<IPopupService>();
        private readonly Action<string> _okAction;

        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(() => Name, ref _name, value); }
        }

        //[ClosePopupCommand(RelatedProperty = "EnterCommand")]
        private ICommand _enterCommand;
        public ICommand EnterCommand => _enterCommand = _enterCommand ?? new RelayCommand<string>(Ok);

        //[ClosePopupCommand(RelatedProperty = "OkCommand")]
        private ICommand _okCommand;
        public ICommand OkCommand => _okCommand = _okCommand ?? new RelayCommand(() => Ok(Name));

        private void Ok(string name)
        {
            PopupService?.Close(this);
            _okAction(name);
        }

        public AskNamePopupViewModel(Action<string> okAction)
        {
            _okAction = okAction;
        }
    }

    public class AskNamePopupViewModelDesignData : AskNamePopupViewModel
    {
        public AskNamePopupViewModelDesignData() : base(name => { })
        {
        }
    }
}
