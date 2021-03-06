﻿using System;

namespace PPC.Services.Popup
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PopupAssociatedViewAttribute : Attribute
    {
        public Type ViewType { get; }

        public PopupAssociatedViewAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}
