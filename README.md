As a proof of concept only, here's one way you could go about doing that. The key here is the `_confirmClosure` bool, because if the user wants to "save and close` then we're going to:

 - Disable the UI to prevent any chance of reentry
 - Asynchronously execute the save.
 - Then, after awaiting the save, call `Close` again, this time with `_confirmClosure` set to `false`.

~~~
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
                            try
                            {
                                Mouse.OverrideCursor = Cursors.Wait;
                                    IsEnabled = false;  // Prevent any more calls.
                                    await DataContext.Save();
                                    _confirmClosure = false;
                                    Close();
                            }
                            finally
                            {
                                Mouse.OverrideCursor = null;
                            }
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
~~~

___

**Minimal VM for Test** 

We'll make a "big" list of 10000 items, then save it to a file upon close.

~~~
public class MainPageViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>(
            Enumerable.Range(1, 10000)
            .Select(_=>new Item { Id = _, Name = $"Item {_}" }));

    public event PropertyChangedEventHandler PropertyChanged;

    internal async Task Save()
    {
        await Task.Run(() =>
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StackOverflow",
                Assembly.GetEntryAssembly()?.GetName()?.Name ?? "SaveThenClose",
                "list-data.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = JsonConvert.SerializeObject(Items, Formatting.Indented);
            File.WriteAllText(path, json);
        });
        // Add a few seconds for good measure, just for demo purposes.
        await Task.Delay(TimeSpan.FromSeconds(2.5));
    }
}

public class Item
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
~~~


```xaml
<Window x:Class="save_list_then_close.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:save_list_then_close"
        mc:Ignorable="d"
        Title="MainWindow" Height="250" Width="400"
        WindowStartupLocation="CenterScreen">
    <Window.DataContext>
        <local:MainPageViewModel/>
    </Window.DataContext>
    <Grid>
        <DataGrid
            Name="dataGrid"
            ItemsSource="{Binding Items}" 
            AutoGenerateColumns="True" 
            IsReadOnly="True" 
            AlternatingRowBackground="LightGray"/>
    </Grid>
</Window>
```