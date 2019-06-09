﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EasyIoc;
using EasyMVVM;
using PPC.Domain;
using PPC.Helpers;
using PPC.Log;
using PPC.Module.Shop.Models;
using PPC.Module.Shop.ViewModels.Popups;
using PPC.Services.Popup;

namespace PPC.Module.Shop.ViewModels
{
    public enum ClientShoppingCartsModes
    {
        List,
        Detail
    }

    public class ClientShoppingCartsViewModel : ObservableObject
    {
        private IPopupService PopupService => IocContainer.Default.Resolve<IPopupService>();
        private ILog Logger => IocContainer.Default.Resolve<ILog>();

        private readonly Action<ShopTransactionItem> _addTransactionAction;
        private readonly Action<decimal, decimal, decimal> _clientCartPaidAction;
        private readonly Action _clientCartReopenedAction;

        public bool HasClientShoppingCartsOpened => ClientShoppingCartsCount > 0;

        public int ClientShoppingCartsCount => Clients.Count;

        public int PaidClientShoppingCartsCount => Clients.Count(x => x.PaymentState == ClientShoppingCartPaymentStates.Paid);

        public int UnpaidClientShoppingCartsCount => Clients.Count(x => x.PaymentState == ClientShoppingCartPaymentStates.Unpaid);
        public bool HasUnpaidClientShoppingCards => Clients.Any(x => x.PaymentState == ClientShoppingCartPaymentStates.Unpaid && x.ShoppingCart.ShoppingCartArticles.Any());

        public decimal ClientShoppingCartsTotal => PaidClientShoppingCartsTotal + UnpaidClientShoppingCartsTotal;

        public decimal PaidClientShoppingCartsTotal => Clients.Where(x => x.PaymentState == ClientShoppingCartPaymentStates.Paid).Sum(x => x.Cash + x.BankCard);

        public decimal UnpaidClientShoppingCartsTotal => Clients.Where(x => x.PaymentState == ClientShoppingCartPaymentStates.Unpaid).Sum(x => x.ShoppingCart.Total);

        private ClientShoppingCartsModes _mode;

        public ClientShoppingCartsModes Mode
        {
            get => _mode;
            protected set { Set(() => Mode, ref _mode, value); }
        }

        #region Clients and client selection

        private ICommand _selectClientCommand;
        public ICommand SelectClientCommand => _selectClientCommand = _selectClientCommand ?? new RelayCommand<ClientShoppingCartViewModel>(client => SelectedClient = client);

        private ClientShoppingCartViewModel _selectedClient;
        public ClientShoppingCartViewModel SelectedClient
        {
            get => _selectedClient;
            protected set
            {
                if (Set(() => SelectedClient, ref _selectedClient, value))
                {
                    if (value == null)
                        Mode = ClientShoppingCartsModes.List;
                    else
                    {
                        Mode = ClientShoppingCartsModes.Detail;
                        value.ShoppingCart.GotFocus();
                    }
                }
            }
        }

        public ObservableCollection<ClientShoppingCartViewModel> Clients { get; }

        #endregion

        #region Add client

        private ICommand _addNewClientCommand;
        public ICommand AddNewClientCommand => _addNewClientCommand = _addNewClientCommand ?? new RelayCommand(AddNewClient);

        private void AddNewClient()
        {
            AskNamePopupViewModel vm = new AskNamePopupViewModel(AddNewClientNameSelected);
            PopupService.DisplayModal(vm, "Client name?");
        }

        private void AddNewClientNameSelected(string name)
        {
            ClientShoppingCartViewModel alreadyExistingClient = Clients.FirstOrDefault(x => string.Equals(x.ClientName, name, StringComparison.InvariantCultureIgnoreCase));
            if (alreadyExistingClient != null)
            {
                Logger.Warning($"A shopping cart with that client name '{name}' has already been opened!");
                PopupService.DisplayError(
                    "Warning",
                    $"A shopping cart with that client name '{name}' has already been opened! Switching to {name} shopping cart.",
                    () => SelectedClient = alreadyExistingClient); // switch to already existing client
            }
            else
            {
                Logger.Info($"Adding new client shopping cart: {name}");

                ClientShoppingCartViewModel newClient = new ClientShoppingCartViewModel(ClientCartPaid, ClientCartReopened)
                {
                    HasFullPlayerInfos = false,
                    ClientName = name,
                };
                Clients.Add(newClient);
                SelectedClient = newClient;
            }
        }

        #endregion

        #region Close client

        private ICommand _closeClientCommand;
        public ICommand CloseClientCommand => _closeClientCommand = _closeClientCommand ?? new RelayCommand<ClientShoppingCartViewModel>(CloseClient);

        private void CloseClient(ClientShoppingCartViewModel client)
        {
            if (client.PaymentState == ClientShoppingCartPaymentStates.Unpaid && client.ShoppingCart.ShoppingCartArticles.Any())
                PopupService.DisplayQuestion($"Close {client.ClientName} shopping cart", $"Client {client.ClientName} has yet not paid, therefore shopping cart cannot be closed.", QuestionActionButton.Ok());
            else
                PopupService.DisplayQuestion($"Close {client.ClientName} shopping cart", "Are you sure ?", QuestionActionButton.Yes(() => CloseClientConfirmed(client)), QuestionActionButton.No());
        }

        private void CloseClientConfirmed(ClientShoppingCartViewModel client)
        {
            SelectedClient = null;
            Mode = ClientShoppingCartsModes.List;
            if (client.ShoppingCart.ShoppingCartArticles.Any())
            {
                // Create a transaction and add it to transactions
                ShopTransactionItem transaction = new ShopTransactionItem
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    Articles = client.ShoppingCart.ShoppingCartArticles.Select(x => new ShopArticleItem
                    {
                        Article = x.Article,
                        Quantity = x.Quantity
                    }).ToList(),
                    Cash = client.Cash,
                    BankCard = client.BankCard
                };
                _addTransactionAction?.Invoke(transaction);
            }
            client.DeleteClientCart();
            Clients.Remove(client);
        }

        #endregion

        public void MergeClient(ClientShoppingCartViewModel from, ClientShoppingCartViewModel to)
        {
            if (from == to)
                return;
            if (from?.PaymentState == ClientShoppingCartPaymentStates.Unpaid
                && to?.PaymentState == ClientShoppingCartPaymentStates.Unpaid)
                PopupService.DisplayQuestion("Merge", $"You are about to merge {from.ClientName} within {to.ClientName}\nAre you sure?", QuestionActionButton.Yes(() => MergeClientConfirmed(from, to)), QuestionActionButton.No());
        }

        private void MergeClientConfirmed(ClientShoppingCartViewModel from, ClientShoppingCartViewModel to)
        {
            // Move articles 'from' -> 'to'
            foreach (ShopArticleItem item in from.ShoppingCart.ShoppingCartArticles)
                to.ShoppingCart.AddArticle(item.Article, item.Quantity);
            // Delete client 'from'
            from.DeleteClientCart();
            Clients.Remove(from);
        }

        public void ReloadClients(Session session)
        {
            SelectedClient = null;
            Clients.Clear(); // TODO: remove handlers

            List<ClientCart> carts = session.ClientCarts;

            if (carts != null)
            {
                foreach (ClientCart cart in carts)
                {
                    try
                    {
                        Logger.Info($"Reload client shopping cart: {cart.ClientName}. Cash: {cart.Cash:C} BankCard: {cart.BankCard:C} IsPaid: {cart.IsPaid}");

                        ClientShoppingCartViewModel client = new ClientShoppingCartViewModel(ClientCartPaid, ClientCartReopened, cart);
                        Clients.Add(client);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception($"Error while creating client {cart.ClientName} from cart", ex);
                        PopupService.DisplayError($"Error while creating client {cart.ClientName} from cart", ex);
                    }
                }
            }
        }

        public bool FindAndRemoveInvalidArticles()
        {
            bool unknownArticleFound = false;
            foreach (ClientShoppingCartViewModel client in Clients.Where(c => c.ShoppingCart.ShoppingCartArticles.Any(a => a.Article == null)))
            {
                client.ShoppingCart.ShoppingCartArticles.RemoveAll(x => x.Article == null);
                unknownArticleFound = true;
            }
            return unknownArticleFound;
        }

        public List<TransactionFullArticle> BuildTransactions()
        {
            List<TransactionFullArticle> transactions = Clients.Select(client => new TransactionFullArticle
            {
                Timestamp = client.PaymentTimestamp,
                Articles = client.ShoppingCart.ShoppingCartArticles.Select(x => new FullArticle
                {
                    Guid = x.Article.Guid,
                    Ean = x.Article.Ean,
                    Description = x.Article.Description,
                    Category = x.Article.Category,
                    SubCategory = x.Article.SubCategory,
                    Price = x.Article.Price,
                    Quantity = x.Quantity
                }).ToList(),
                Cash = client.Cash,
                BankCard = client.BankCard,
                DiscountPercentage = client.DiscountPercentage
            }).ToList();
            return transactions;
        }

        public ClientShoppingCartsViewModel(Action<ShopTransactionItem> addTransactionAction, Action<decimal, decimal, decimal> clientCartPaidAction, Action clientCartReopenedAction)
        {
            _addTransactionAction = addTransactionAction;
            _clientCartPaidAction = clientCartPaidAction;
            _clientCartReopenedAction = clientCartReopenedAction;

            Clients = new ObservableCollection<ClientShoppingCartViewModel>();
            Clients.CollectionChanged += (sender, args) =>
            {
                RefreshCounters();
            };
        }

        private void RefreshCounters()
        {
            RaisePropertyChanged(() => ClientShoppingCartsCount);
            RaisePropertyChanged(() => PaidClientShoppingCartsCount);
            RaisePropertyChanged(() => UnpaidClientShoppingCartsCount);
            RaisePropertyChanged(() => HasUnpaidClientShoppingCards);
            RaisePropertyChanged(() => ClientShoppingCartsTotal);
            RaisePropertyChanged(() => PaidClientShoppingCartsTotal);
            RaisePropertyChanged(() => UnpaidClientShoppingCartsTotal);
            RaisePropertyChanged(() => HasClientShoppingCartsOpened);
        }

        // TODO: RefreshCounters should be called when an item is added in shopping cart

        private void ClientCartReopened()
        {
            RefreshCounters();

             _clientCartReopenedAction?.Invoke();
        }

        private void ClientCartPaid(decimal cash, decimal bankCard, decimal discountPercentage)
        {
            RefreshCounters();

            _clientCartPaidAction?.Invoke(cash, bankCard, discountPercentage);
        }
    }

    public class ClientShoppingCartsViewModelDesignData : ClientShoppingCartsViewModel
    {
        public ClientShoppingCartsViewModelDesignData() : base(_ => { }, (a, b, c) => { }, () => { })
        {
            Clients.Clear();
            Clients.AddRange(new List<ClientShoppingCartViewModel>
            {
                new ClientShoppingCartViewModelDesignData
                {
                    ClientName = "un super long nom0",
                    //PaymentState = ClientShoppingCartPaymentStates.Unpaid
                },

                new ClientShoppingCartViewModelDesignData
                {
                    ClientName = "Joel",
                   // PaymentState = ClientShoppingCartPaymentStates.Paid
                },
                new ClientShoppingCartViewModelDesignData
                {
                    ClientName = "Pouet",
                    //PaymentState = ClientShoppingCartPaymentStates.Unpaid
                }
            });
            Clients.AddRange(Enumerable.Range(1, 5).Select(x => new ClientShoppingCartViewModelDesignData
            {
                ClientName = $"un super long nom[{x}]",
                //PaymentState = x % 2 == 0 ? ClientShoppingCartPaymentStates.Unpaid : ClientShoppingCartPaymentStates.Paid
            }));
        }
    }
}
