﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using EasyIoc;
using EasyMVVM;
using PPC.Data.Contracts;
using PPC.Data.Players;
using PPC.Helpers;
using PPC.Messages;
using PPC.Module.Players.Models;
using PPC.Services.Popup;

namespace PPC.Module.Players.ViewModels
{
    public class PlayersViewModel : ObservableObject
    {
        private IPopupService PopupService => IocContainer.Default.Resolve<IPopupService>();

        #region Filtered players

        private IEnumerable<PlayerItem> _filteredPlayers;
        public IEnumerable<PlayerItem> FilteredPlayers
        {
            get { return _filteredPlayers;}
            set { Set(() => FilteredPlayers, ref _filteredPlayers, value); }
        }

        private string _filter;
        public string Filter
        {
            get { return _filter; }
            set
            {
                if (Set(() => Filter, ref _filter, value))
                    FilterPlayers();
            }
        }

        private bool FilterPlayer(PlayerItem p)
        {
            if (string.IsNullOrWhiteSpace(Filter))
                return true;
            //http://stackoverflow.com/questions/359827/ignoring-accented-letters-in-string-comparison/7720903#7720903
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(p.FirstName, Filter, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0
                || CultureInfo.CurrentCulture.CompareInfo.IndexOf(p.LastName, Filter, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0
                || CultureInfo.CurrentCulture.CompareInfo.IndexOf(p.DCINumber, Filter, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0;
        }

        private void FilterPlayers()
        {
            if (Players == null)
                return;
             FilteredPlayers = Players?.Where(FilterPlayer).ToList();
            //// If only one player, select it
            //if (FilteredPlayers.Count() == 1)
            //    SelectedPlayer = FilteredPlayers.First();
            SelectedPlayer = FilteredPlayers.FirstOrDefault();
        }

        #endregion

        private ObservableCollection<PlayerItem> _players;
        public ObservableCollection<PlayerItem> Players
        {
            get { return _players; }
            protected set
            {
                if (Set(() => Players, ref _players, value))
                    FilterPlayers();
            }
        }

        private PlayerItem _selectedPlayer;
        public PlayerItem SelectedPlayer
        {
            get { return _selectedPlayer; }
            set { Set(() => SelectedPlayer, ref _selectedPlayer, value); }
        }

        #region Load

        private ICommand _loadCommand;
        public ICommand LoadCommand => _loadCommand = _loadCommand ?? new RelayCommand(() => Load(false));

        private void Load(bool onlyIfExists)
        {
            try
            {
                string filename = ConfigurationManager.AppSettings["PlayersPath"];
                if (onlyIfExists && !File.Exists(filename))
                    return;
                List<Player> players = IocContainer.Default.Resolve<IPlayersDb>().Load(filename);
                Players = new ObservableCollection<PlayerItem>(players.Select(x => new PlayerItem
                {
                    DCINumber = x.DCINumber,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    MiddleName = x.MiddleName,
                    CountryCode = x.CountryCode,
                    IsJudge = x.IsJudge
                }).OrderBy(x => x.FirstName, new EmptyStringsAreLast()));
            }
            catch (Exception ex)
            {
                PopupService.DisplayError("Error while loading player file", ex);
            }
        }

        #endregion

        #region Save

        private ICommand _saveCommand;

        public ICommand SaveCommand  => _saveCommand = _saveCommand ?? new RelayCommand(Save);

        private void Save()
        {
            try
            {
                List<Player> players = Players.Select(x => new Player
                {
                    DCINumber = x.DCINumber,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    MiddleName = x.MiddleName,
                    CountryCode = x.CountryCode,
                    IsJudge = x.IsJudge
                }).ToList();
                IocContainer.Default.Resolve<IPlayersDb>().Save(ConfigurationManager.AppSettings["PlayersPath"], players);
                Load(false); //TODO crappy workaround to reset row.IsNewItem
            }
            catch (Exception ex)
            {
                PopupService.DisplayError("Error while saving player file", ex);
            }
        }

        #endregion

        #region Select player

        private ICommand _selectPlayerCommand;
        public ICommand SelectPlayerCommand => _selectPlayerCommand = _selectPlayerCommand ?? new RelayCommand<PlayerItem>(pm => SelectPlayer(pm, true));

        private ICommand _selectPlayerWithModifierCommand;
        public ICommand SelectPlayerWithModifierCommand => _selectPlayerWithModifierCommand = _selectPlayerWithModifierCommand ?? new RelayCommand<PlayerItem>(pm => SelectPlayer(pm, false));

        private void SelectPlayer(PlayerItem player, bool switchToShop)
        {
            if (player != null)
            {
                Mediator.Default.Send(new PlayerSelectedMessage
                {
                    DciNumber = player.DCINumber,
                    FirstName = player.FirstName,
                    LastName = player.LastName,
                    SwitchToShop = switchToShop
                });
                Filter = string.Empty;
            }
        }

        #endregion

        public PlayersViewModel()
        {
            if (!DesignMode.IsInDesignModeStatic)
                Load(true);
        }
    }

    public class PlayersViewModelDesignData : PlayersViewModel
    {
        public PlayersViewModelDesignData()
        {
            Players = new ObservableCollection<PlayerItem>
            {
                new PlayerItem
                {
                    DCINumber = "123456789",
                    FirstName = "pouet",
                    LastName = "taratata",
                    CountryCode = "BE"
                },
                 new PlayerItem
                {
                    DCINumber = "9876543",
                    FirstName = "tsekwa",
                    LastName = "gamin",
                    CountryCode = "FR"
                }
            };
        }
    }
}
