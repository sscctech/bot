﻿using System;
using System.Diagnostics;
#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#else
using PixelLab.Contracts;
#endif
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PixelLab.Common
{
    public class StringCommand
    {
        public static readonly DependencyProperty CommandProperty = DependencyPropHelper.RegisterAttached<StringCommand, ButtonBase, string>("Command", element_commandChanged);

        public static string GetCommand(FrameworkElement element)
        {
            Contract.Requires(element != null);
            return (string)element.GetValue(CommandProperty);
        }

        public static void SetCommand(FrameworkElement element, string value)
        {
            element.SetValue(CommandProperty, value);
        }

        private static void element_commandChanged(ButtonBase source, string newValue, string oldValue)
        {
            Debug.Assert(source != null);
            Debug.Assert(!newValue.IsNullOrWhiteSpace());
            Debug.Assert(oldValue == null);

            source.Loaded += commandElement_loaded;
        }

        private static void commandElement_loaded(object sender, EventArgs args)
        {
            var buttonBase = (ButtonBase)sender;
            buttonBase.Loaded -= commandElement_loaded;
            tryWire(buttonBase);
        }

        private static void tryWire(ButtonBase source)
        {
            Contract.Requires(source != null);
            var commandName = GetCommand(source);
            var value = (from anscestor in source.GetAncestors().OfType<ICommandProxy>()
                         let command = TryGetCommand(anscestor, source, commandName)
                         where command != null
                         select command).FirstOrDefault();

            if (value != null)
            {
                source.Command = value;
            }
            else
            {
                DebugTrace.WriteLine("Could not bind command string '{0}' to {1}", commandName, source);
                source.Command = DeadCommand.Instance;
            }
        }

        private static ICommand TryGetCommand(ICommandProxy proxy, DependencyObject source, string name)
        {
            Contract.Requires(proxy != null);
            Contract.Requires(source != null);
            Contract.Requires(!name.IsNullOrWhiteSpace());

            var realSource = proxy.GetCommandOwner(source);
            if (realSource != null)
            {
                var propInfo = (from property in realSource.GetType().GetProperties()
                                where property.CanRead
                                where property.Name == name
                                where typeof(ICommand).IsAssignableFrom(property.PropertyType)
                                select property).SingleOrDefault();

                if (propInfo != null)
                {
                    return (ICommand)propInfo.GetGetMethod().Invoke(realSource, new object[0]);
                }
            }
            return null;
        }
    }
}
