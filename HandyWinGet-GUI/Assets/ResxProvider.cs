using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HandyWinget_GUI.Assets
{
    public class ResxProvider : ILocalizationProvider
    {
        public IEnumerable<CultureInfo> Cultures => throw new NotImplementedException();

        public object Localize(string key)
        {
            return Assets.Languages.Lang.ResourceManager.GetObject(key);
        }
    }
}
