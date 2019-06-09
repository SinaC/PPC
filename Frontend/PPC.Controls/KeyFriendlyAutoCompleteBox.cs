﻿using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PPC.Controls
{
    public class KeyFriendlyAutoCompleteBox : AutoCompleteBox
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                TextBox textBox = e.OriginalSource as TextBox;
                textBox?.SelectAll();
            }
            e.Handled = false;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            textBox?.SelectAll();

            if (e.OriginalSource is KeyFriendlyAutoCompleteBox autoCompleteBox)
            {
                TextBox innerTextBox = Helpers.VisualTreeHelpers.FindVisualChild<TextBox>(autoCompleteBox);
                if (innerTextBox != null)
                {
                    Dispatcher.BeginInvoke((Action) delegate
                    {
                        innerTextBox.Focus();
                        Keyboard.Focus(innerTextBox);
                    }, DispatcherPriority.Input);
                }
            }
        }
    }
}
