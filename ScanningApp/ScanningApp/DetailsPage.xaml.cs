using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ScanningApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DetailsPage : ContentPage
    {
        ObservableCollection<string> scannedCodes = new ObservableCollection<string>();

        public DetailsPage()
        {
            InitializeComponent();
            detailsList.ItemsSource = DataStore.ScannedCodes;
            UpdateCount();

            // Listen to changes
            DataStore.ScannedCodes.CollectionChanged += ScannedCodes_CollectionChanged;
        }

        private void ScannedCodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCount();
        }

        private void UpdateCount()
        {
            CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DataStore.ScannedCodes.CollectionChanged -= ScannedCodes_CollectionChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            detailsList.ItemsSource = null;
            detailsList.ItemsSource = DataStore.ScannedCodes;
            UpdateCount();
        }
    }
}
