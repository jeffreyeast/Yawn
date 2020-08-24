//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Yawn
{
    public static class CustomCommands
    {
        public readonly static RoutedUICommand CloseCommand = new RoutedUICommand(Properties.Resources.CloseText, Properties.Resources.CloseText, typeof(CustomCommands));
        public readonly static RoutedUICommand DockCommand = new RoutedUICommand(Properties.Resources.DockText, Properties.Resources.DockText, typeof(CustomCommands));
        public readonly static RoutedUICommand FloatCommand = new RoutedUICommand(Properties.Resources.FloatText, Properties.Resources.FloatText, typeof(CustomCommands));
        public readonly static RoutedUICommand MaximizeCommand = new RoutedUICommand(Properties.Resources.MaximizeText, Properties.Resources.MaximizeText, typeof(CustomCommands));
        public readonly static RoutedUICommand MinimizeCommand = new RoutedUICommand(Properties.Resources.MinimizeText, Properties.Resources.MinimizeText, typeof(CustomCommands));
        public readonly static RoutedUICommand RestoreCommand = new RoutedUICommand(Properties.Resources.RestoreText, Properties.Resources.RestoreText, typeof(CustomCommands));
        public readonly static RoutedCommand SelectCommand = new RoutedCommand();
        public readonly static RoutedUICommand ShowCommand = new RoutedUICommand(Properties.Resources.ShowText, Properties.Resources.ShowText, typeof(CustomCommands));
        public readonly static RoutedCommand TogglePinCommand = new RoutedCommand();
    }
}
