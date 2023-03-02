using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;

namespace wpf_memory_leaks;

public class LeakyTests
{
    [UIFact]
    public async Task BindingSource()
    {
        WeakReference textReference, watcherReference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Source = data };
            var text = new TextBlock();
            textReference = new WeakReference(text);
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
            Assert.Single(data.Subscribers);

            // Object subscribing
            watcherReference = new WeakReference(data.Subscribers[0].Target);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(textReference.IsAlive, "TextBlock should not be alive!");

        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(watcherReference.IsAlive, $"Subscriber {watcherReference.Target} should not be alive!");
        Assert.Empty(data.Subscribers);
    }

    [UIFact]
    public async Task BindingDataContext()
    {
        WeakReference textReference, watcherReference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Mode = BindingMode.OneWay };
            var text = new TextBlock();
            textReference = new WeakReference(text);
            text.DataContext = data;
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
            Assert.Single(data.Subscribers);

            // Object subscribing
            watcherReference = new WeakReference(data.Subscribers[0].Target);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(textReference.IsAlive, "TextBlock should not be alive!");

        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(watcherReference.IsAlive, $"Subscriber {watcherReference.Target} should not be alive!");
        Assert.Empty(data.Subscribers);
    }

    class MySweetObject : INotifyPropertyChanged
    {
        string _text = "";

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public Delegate[] Subscribers => PropertyChanged?.GetInvocationList() ?? Array.Empty<Delegate>();

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}