using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ScanningApp
{
    public static class DataStore
    {

        public static ObservableCollection<string> ScannedCodes { get; set; } = new ObservableCollection<string>();
    }
}

