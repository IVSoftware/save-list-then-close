using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace save_list_then_close
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                if(dataGrid.Columns
                    .OfType<DataGridTextColumn>()
                    .FirstOrDefault(c => c.Header.ToString() == "Name") is { } column)
                {
                    // Autosize "Fill" for item name
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                }
            };
            Closing += async (sender, e) =>
            {
                if (_confirmClosure)
                {
                    switch (
                        MessageBox.Show(
                            "Do you want to save before closing?",
                            "Confirm Exit",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Yes:
                            e.Cancel = true;
                            await Dispatcher.BeginInvoke(async () =>
                            {
                                IsEnabled = false;  // Prevent any more calls.
                                await DataContext.Save();
                                _confirmClosure = false;
                                Close();
                            });
                            break;

                        case MessageBoxResult.No:
                            break;

                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            break;
                    }
                }
            };
        }
        bool _confirmClosure = true;
        new MainPageViewModel DataContext => (MainPageViewModel)base.DataContext;
    }
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>(
                Enumerable.Range(1, 10000)
                .Select(_=>new Item { Id = _, Name = $"Item {_}" }));

        public event PropertyChangedEventHandler PropertyChanged;

        internal async Task Save()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await Task.Run(() =>
                {
                    var path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "StackOverflow",
                        Assembly.GetEntryAssembly()?.GetName()?.Name ?? "SaveThenClose");
                        var json = JsonConvert.SerializeObject(Items);
                    File.WriteAllText(path, json);
                });
                // Add a few seconds for good measure, just for demo purposes.
                await Task.Delay(TimeSpan.FromSeconds(2.5));
            }
            finally
            {
                // Reset the cursor to default
                Mouse.OverrideCursor = null;
            }
        }
    }

    public class Item
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}