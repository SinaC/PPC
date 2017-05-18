﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EasyMVVM;
using PPC.Data.Contracts;
using PPC.Module.Shop.Models;
using PPC.Module.Shop.Views.Popups;
using PPC.Services.Popup;

namespace PPC.Module.Shop.ViewModels.Popups
{
    // TODO: don't pay and delete transaction if each articles has been removed
    [PopupAssociatedView(typeof(TransactionEditorPopup))]
    public class TransactionEditorPopupViewModel : ObservableObject
    {
        private IPopupService PopupService => EasyIoc.IocContainer.Default.Resolve<IPopupService>();
        private readonly Action<ShopTransactionItem> _saveTransactionAction;

        private readonly ShopTransactionItem _originalTransactionItem;

        #region Articles

        public decimal Total => Articles.Sum(x => x.Total);

        private ObservableCollection<ShopArticleItem> _articles;
        public ObservableCollection<ShopArticleItem> Articles
        {
            get { return _articles; }
            protected set { Set(() => Articles, ref _articles, value); }
        }

        #region Delete article from cart

        private ICommand _deleteArticleCommand;
        public ICommand DeleteArticleCommand => _deleteArticleCommand = _deleteArticleCommand ?? new RelayCommand<ShopArticleItem>(DeleteArticle);

        private void DeleteArticle(ShopArticleItem item)
        {
            Articles.Remove(item);
            RaisePropertyChanged(() => Total);
        }

        #endregion

        #region Increment article in cart

        private ICommand _incrementArticleCommand;
        public ICommand IncrementArticleCommand => _incrementArticleCommand = _incrementArticleCommand ?? new RelayCommand<ShopArticleItem>(IncrementArticle);

        private void IncrementArticle(ShopArticleItem item)
        {
            item.Quantity++;
            RaisePropertyChanged(() => Total);
        }

        #endregion

        #region Decrement article in cart

        private ICommand _decrementArticleCommand;
        public ICommand DecrementArticleCommand => _decrementArticleCommand = _decrementArticleCommand ?? new RelayCommand<ShopArticleItem>(DecrementArticle);

        private void DecrementArticle(ShopArticleItem item)
        {
            if (item.Quantity == 1)
                DeleteArticle(item);
            else
            {
                item.Quantity--;
                RaisePropertyChanged(() => Total);
            }
        }

        #endregion

        #endregion

        #region Original transaction values

        private decimal _cash;
        public decimal Cash
        {
            get { return _cash; }
            protected set { Set(() => Cash, ref _cash, value); }
        }

        private decimal _bankCard;
        public decimal BankCard
        {
            get { return _bankCard; }
            protected set { Set(() => BankCard, ref _bankCard, value); }
        }

        private decimal _discountPercentage;
        public decimal DiscountPercentage
        {
            get { return _discountPercentage; }
            protected set { Set(() => DiscountPercentage, ref _discountPercentage, value); }
        }

        #endregion

        private ICommand _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand = _refreshCommand ?? new RelayCommand(() => Initialize(_originalTransactionItem));

        #region Payment

        #region Cash payment

        private ICommand _cashCommand;
        public ICommand CashCommand => _cashCommand = _cashCommand ?? new RelayCommand(() => DisplayPaymentPopup(true));

        #endregion

        #region Bank card payment

        private ICommand _bankCardCommand;
        public ICommand BankCardCommand => _bankCardCommand = _bankCardCommand ?? new RelayCommand(() => DisplayPaymentPopup(false));

        #endregion

        private void DisplayPaymentPopup(bool isCashFirst)
        {
            PaymentPopupViewModel vm = new PaymentPopupViewModel(Total, isCashFirst, PaymentDone);
            PopupService.DisplayModal(vm, "Payment");
        }

        private void PaymentDone(decimal cash, decimal bankCard, decimal discountPercentage)
        {
            ShopTransactionItem transactionItem = new ShopTransactionItem
            {
                Id = _originalTransactionItem.Id,
                Timestamp = DateTime.Now,
                Articles = Articles.ToList(),
                Cash = cash,
                BankCard = bankCard,
                DiscountPercentage = discountPercentage
            };
            _saveTransactionAction?.Invoke(transactionItem);
            //
            PopupService?.Close(this);
        }

        #endregion

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand = _cancelCommand ?? new RelayCommand(Cancel);
        private void Cancel()
        {
            PopupService?.Close(this);
        }

        public TransactionEditorPopupViewModel(ShopTransactionItem transactionItem, Action<ShopTransactionItem> saveTransactionAction)
        {
            _originalTransactionItem = transactionItem;
            _saveTransactionAction = saveTransactionAction;
            Initialize(transactionItem);
        }

        private void Initialize(ShopTransactionItem transactionItem)
        {
            Cash = transactionItem.Cash;
            BankCard = transactionItem.BankCard;
            DiscountPercentage = transactionItem.DiscountPercentage;
            Articles = new ObservableCollection<ShopArticleItem>(transactionItem.Articles.Select(x => new ShopArticleItem
            {
                Quantity = x.Quantity,
                Article = x.Article
            }));
        }
    }

    public class TransactionEditorPopupViewModelDesignData : TransactionEditorPopupViewModel
    {
        public TransactionEditorPopupViewModelDesignData() : base(new ShopTransactionItem
        {
            Cash = 94,
            BankCard = 5,
            DiscountPercentage = 0.10m,
            Articles = new List<ShopArticleItem>
            {
                new ShopArticleItem
                {
                    Article = new Article
                    {
                        Ean = "1111111",
                        Description = "Article1",
                        Price = 10
                    },
                    Quantity = 2,
                },
                new ShopArticleItem
                {
                    Article = new Article
                    {
                        Ean = "222222222",
                        Description = "Article2",
                        Price = 20
                    },
                    Quantity = 3,
                },
                new ShopArticleItem
                {
                    Article = new Article
                    {
                        Ean = "33333333",
                        Description = "Article3",
                        Price = 30
                    },
                    Quantity = 1,
                }
            }
        }, _ => { })
        {
        }
    }
}
