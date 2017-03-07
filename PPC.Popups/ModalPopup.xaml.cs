﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace PPC.Popup
{
    /// <summary>
    /// Interaction logic for ModalPopup.xaml
    /// </summary>
    public partial class ModalPopup : UserControl, INotifyPropertyChanged, IPopup, ISaveNavigationAndFocusPopup
    {
        #region IPopup

        public Guid Guid { get; }

        #endregion

        #region ISaveNavigationAndFocusPopup

        public KeyboardNavigationMode SavedTabNavigationMode { get; set; }

        public KeyboardNavigationMode SavedDirectionalNavigationMode { get; set; }

        public IInputElement SavedFocusedElement { get; set; }

        #endregion

        public Func<bool> CloseConfirmation { get; set; }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public ModalPopup()
        {
            InitializeComponent();

            // Inner UserControl will be loaded when setting DataContext

            // Unique id
            Guid = Guid.NewGuid();
        }

        private void ThumbResize_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = ActualHeight + e.VerticalChange;
            double xadjust = ActualWidth + e.HorizontalChange;
            if (xadjust >= 0 && yadjust >= 0)
            {
                Width = xadjust;
                Height = yadjust;
            }
        }

        private void ThumbMove_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            PopupService.Move(this, e.HorizontalChange, e.VerticalChange);
        }
        
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            bool closeAllowed = true;
            if (CloseConfirmation != null)
                closeAllowed = CloseConfirmation();
            if (closeAllowed)
                PopupService.Close(this as IPopup);
        }

        private IPopupService PopupService => FindParent<IPopupService>(this);

        private static T FindParent<T>(DependencyObject child)
            where T : class
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null)
                return default(T);

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}