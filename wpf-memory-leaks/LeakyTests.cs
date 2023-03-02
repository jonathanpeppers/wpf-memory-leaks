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
        WeakReference reference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Source = data };
            var text = new TextBlock();
            reference = new WeakReference(text);
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
            Assert.Equal(1, data.SubscriberCount);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(reference.IsAlive, "TextBlock should not be alive!");
        Assert.Equal(0, data.SubscriberCount);
    }

    [UIFact]
    public async Task BindingDataContext()
    {
        WeakReference reference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Mode = BindingMode.OneWay };
            var text = new TextBlock();
            reference = new WeakReference(text);
            text.DataContext = data;
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
            Assert.Equal(1, data.SubscriberCount);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(reference.IsAlive, "TextBlock should not be alive!");
        Assert.Equal(0, data.SubscriberCount);
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

        public int SubscriberCount => PropertyChanged?.GetInvocationList().Length ?? 0;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}