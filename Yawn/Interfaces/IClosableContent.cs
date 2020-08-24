//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn.Interfaces
{
    /// <summary>
    /// Optional interface implemented by content that wants to be notified of closures
    /// </summary>
    public interface IClosableContent
    {
        void OnClosed(EventArgs e);
        void OnClosing(CancelEventArgs e);
    }
}
