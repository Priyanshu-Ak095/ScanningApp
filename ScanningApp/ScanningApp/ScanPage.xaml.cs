using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace ScanningApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanPage : ContentPage
    {
        //ObservableCollection<string> scannedCodes = new ObservableCollection<string>();
        //ObservableCollection<string> scannedCodes => DataStore.ScannedCodes;
        ObservableCollection<string> scannedCodes = new ObservableCollection<string>();

        public ScanPage()
        {
            InitializeComponent();
            scanList.ItemsSource = DataStore.ScannedCodes;
            UpdateCountLabel();

        }

        // Handles manual input validation
        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            string enteredCode = entry.Text?.Trim();

            // Only act if it reaches 16 digits
            if (enteredCode.Length == 16 && enteredCode.All(char.IsDigit))
            {
                if (DataStore.ScannedCodes.Contains(enteredCode))
                {
                    DisplayAlert("Duplicate", "This code is already scanned.", "OK");
                }
                else
                {
                    DataStore.ScannedCodes.Add(enteredCode);
                    scanList.ItemsSource = null;
                    scanList.ItemsSource = DataStore.ScannedCodes;
                    CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
                }

                entry.Text = ""; // Clear after adding
            }
        }

        // Handles QR scan tap
        private async void OnScanTapped(object sender, EventArgs e)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                // Add a small delay to ensure the camera initializes
                await Task.Delay(500);

                // Use MainThread to ensure UI sync
                Device.BeginInvokeOnMainThread(() =>
                {
                    var scanPage = new ZXing.Net.Mobile.Forms.ZXingScannerPage();

                    scanPage.OnScanResult += (result) =>
                    {
                        scanPage.IsScanning = false;

                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await Navigation.PopAsync();

                            string scannedText = result.Text;

                            if (scannedText.Length == 16 && scannedText.All(char.IsDigit))
                            {
                                if (DataStore.ScannedCodes.Contains(scannedText))
                                {
                                    await DisplayAlert("Duplicate", "This code is already scanned.", "OK");
                                }
                                else
                                {
                                    DataStore.ScannedCodes.Add(scannedText);
                                    scanList.ItemsSource = null;
                                    scanList.ItemsSource = DataStore.ScannedCodes;
                                    CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
                                }
                            }
                            else
                            {
                                await DisplayAlert("Invalid", "QR code must be 16-digit numeric.", "OK");
                            }
                        });
                    };

                    Navigation.PushAsync(scanPage); // Navigate *after* permission and delay
                });
            }
            else
            {
                await DisplayAlert("Permission Denied", "Camera permission is required to scan QR codes.", "OK");
            }
        }

        // Add code to list and update UI
        private void AddCode(string code)
        {
            if (DataStore.ScannedCodes.Contains(code))
            {
                DisplayAlert("Duplicate", "This ID is already scanned.", "OK");
                return;
            }
            UpdateCountLabel();
        }

        // Delete selected item
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var selectedItem = scanList.SelectedItem;

            if (selectedItem == null)
            {
                await DisplayAlert("Delete", "Please tap an item in the list to select it before deleting.", "OK");
                return;
            }

            string selectedCode = selectedItem.ToString();

            // Check if item exists in the DB
            var dbItems = await DatabaseService.GetItems();
            var isInDatabase = dbItems.Any(i => i.Code == selectedCode);

            bool confirm;

            if (isInDatabase)
            {
                confirm = await DisplayAlert("Delete from DB", $"This item was saved. Do you want to permanently delete '{selectedCode}' from the database?", "Yes", "No");
            }
            else
            {
                confirm = await DisplayAlert("Delete", $"Do you want to delete '{selectedCode}' from the list?", "Yes", "No");
            }

            if (confirm)
            {
                // Remove from in-memory list
                DataStore.ScannedCodes.Remove(selectedCode);

                // Remove from DB if needed
                if (isInDatabase)
                {
                    var itemToDelete = dbItems.FirstOrDefault(i => i.Code == selectedCode);
                    if (itemToDelete != null)
                    {
                        await DatabaseService.DeleteItem(itemToDelete.Id);
                    }
                }

                // Refresh list UI
                scanList.ItemsSource = null;
                scanList.ItemsSource = DataStore.ScannedCodes;
                CountLabel.Text = DataStore.ScannedCodes.Count.ToString();

                // Optional: Clear selection to remove highlight
                scanList.SelectedItem = null;
            }
        }

        // Clear the list
        private void OnClearClicked(object sender, EventArgs e)
        {
            DataStore.ScannedCodes.Clear();
            UpdateCountLabel();
        }

        // Show count
        private void UpdateCountLabel()
        {
            // Label is 3rd in button row → You can replace with a named label if needed
            //var label = ((StackLayout)((StackLayout)this.Content).Children[2]).Children[2] as Label;
            //label.Text = scannedCodes.Count.ToString();
            CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
            //CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
        }

        // Save to DB
        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            var existingItems = await DatabaseService.GetItems();
            var existingCodes = existingItems.Select(i => i.Code).ToList();

            List<string> duplicates = new List<string>();
            List<string> toSave = new List<string>();

            foreach (var code in DataStore.ScannedCodes)
            {
                if (existingCodes.Contains(code))
                {
                    duplicates.Add(code);
                }
                else
                {
                    toSave.Add(code);
                    await DatabaseService.AddItem(code);
                }
            }

            string message = "";

            if (toSave.Count > 0)
                message += $"✅ Saved ID(s): {string.Join(", ", toSave)}\n\n";

            if (duplicates.Count > 0)
                message += $"⚠️ Already saved ID(s): {string.Join(", ", duplicates)}";

            if (string.IsNullOrEmpty(message))
                message = "No items to save.";

            await DisplayAlert("Submission Result", message, "OK");

            // Clear only the saved items from the list
            foreach (var saved in toSave)
            {
                DataStore.ScannedCodes.Remove(saved);
            }

            scanList.ItemsSource = null;
            scanList.ItemsSource = DataStore.ScannedCodes;
            CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
        }

        // Reload from DB
        private async void OnReloadClicked(object sender, EventArgs e)
        {
            var items = await DatabaseService.GetItems();
            DataStore.ScannedCodes.Clear();

            foreach (var item in items)
            {
                DataStore.ScannedCodes.Add(item.Code);
            }

            scanList.ItemsSource = null;
            scanList.ItemsSource = DataStore.ScannedCodes;
            CountLabel.Text = DataStore.ScannedCodes.Count.ToString();
        }

        // Go to Details tab
        private async void OnNextClicked(object sender, EventArgs e)
        {
            // Optional: You can navigate or scroll to tab
            await DisplayAlert("Info", "Swipe to see the Details tab.", "OK");
        }
    }
}
