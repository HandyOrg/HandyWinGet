using System;
using System.Collections.Generic;
using System.Windows;

namespace HandyWinget_GUI
{
    public static class AddOnUi
    {
        public static void AddOnUI<T>(this ICollection<T> collection, T item)
        {
            Action<T> addMethod = collection.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }
    }
}
