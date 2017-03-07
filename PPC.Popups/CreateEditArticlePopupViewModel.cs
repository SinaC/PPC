﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using EasyMVVM;
using PPC.Data.Contracts;

namespace PPC.Popups
{
    public class CreateEditArticlePopupViewModel : ObservableObject
    {
        private readonly IPopupService _popupService;
        private readonly Action<CreateEditArticlePopupViewModel> _saveArticleAction;

        // true: edition  false: creation
        private bool _isEdition;
        public bool IsEdition {
            get { return _isEdition; }
            set { Set(() => IsEdition, ref _isEdition, value); }
        }

        private string _ean;
        public string Ean
        {
            get { return _ean; }
            set { Set(() => Ean, ref _ean, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(() => Description, ref _description, value); }
        }

        public ObservableCollection<string> Categories { get; private set; }

        private string _category;
        public string Category
        {
            get { return _category; }
            set
            {
                if (Set(() => Category, ref _category, value))
                {
                    if (string.Compare(Category, "food", StringComparison.InvariantCultureIgnoreCase) == 0
                        || string.Compare(Category, "drink", StringComparison.InvariantCultureIgnoreCase) == 0) 
                        VatRate = VatRates.FoodDrink;
                    else
                        VatRate = VatRates.Other;
                }
            }
        }

        public ObservableCollection<string> Producers { get; private set; }

        private string _producer;
        public string Producer
        {
            get { return _producer; }
            set { Set(() => Producer, ref _producer, value); }
        }

        private decimal _supplierPrice;
        public decimal SupplierPrice
        {
            get { return _supplierPrice; }
            set { Set(() => SupplierPrice, ref _supplierPrice, value); }
        }

        private decimal _price;
        public decimal Price
        {
            get { return _price; }
            set { Set(() => Price, ref _price, value); }
        }

        public Dictionary<VatRates, string> VatRateList { get; private set; }

        private VatRates _vatRate;
        public VatRates VatRate
        {
            get {  return _vatRate;}
            set { Set(() => VatRate, ref _vatRate, value); }
        }

        private int _stock;
        public int Stock
        {
            get { return _stock; }
            set { Set(() => Stock, ref _stock, value); }
        }

        private ICommand _createEditArticleCommand;
        public ICommand SaveArticleCommand => _createEditArticleCommand = _createEditArticleCommand ?? new RelayCommand(SaveArticle);

        private void SaveArticle()
        {
            _popupService.Close(this);
            _saveArticleAction(this);
        }

        public CreateEditArticlePopupViewModel(IPopupService popupService, IEnumerable<string> categories, IEnumerable<string> producers, Action<CreateEditArticlePopupViewModel> saveArticleAction)
        {
            _popupService = popupService;
            _saveArticleAction = saveArticleAction;
            Categories = new ObservableCollection<string>(categories);
            Producers = new ObservableCollection<string>(producers);
            VatRateList = Enum.GetValues(typeof(VatRates)).Cast<VatRates>().ToDictionary(x => x, x => GetEnumDescription(x));

            Stock = 9999;
        }

        private string GetEnumDescription(Enum value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes =(DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }

    public class CreateEditArticlePopupViewModelDesignData : CreateEditArticlePopupViewModel
    {
        public CreateEditArticlePopupViewModelDesignData() : base(null, new [] {""}, new[] { "" }, _ => { })
        {
            Ean = "1111111111111";
            Description = "Article1";
            Category = "Cards";
            Producer = "MTG";
            SupplierPrice = 5.45m;
            Price = 8;
            Stock = 7;
        }
    }
}
