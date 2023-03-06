using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace wpf_memory_leaks;

public class LeakyTests
{
    [UIFact]
    public async Task BindingSource()
    {
        var textBlocks = new List<WeakReference>();
        WeakReference watcherReference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Source = data };
            var text = new TextBlock();
            textBlocks.Add(new WeakReference(text));
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal(data.Text, text.Text);
            text.SetBinding(TextBlock.FontFamilyProperty, binding);
            Assert.Equal(data.Text, text.FontFamily.ToString());

            text = new TextBlock();
            textBlocks.Add(new WeakReference(text));
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal(data.Text, text.Text);
            text.SetBinding(TextBlock.FontFamilyProperty, binding);
            Assert.Equal(data.Text, text.FontFamily.ToString());

            // Single object subscribing
            Assert.Single(data.Subscribers);
            watcherReference = new WeakReference(data.Subscribers[0].Target);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        foreach (var textReference in textBlocks)
        {
            Assert.False(textReference.IsAlive, "TextBlock should not be alive!");
        }

        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // 1 Global watcher is still alive
        Assert.Single(data.Subscribers);
        var watcher = watcherReference.Target;
        Assert.NotNull(watcher);
        var currentManager = MyManager.GetCurrentManager(watcher.GetType());
        Assert.Same(currentManager, watcher);
    }

    [UIFact]
    public async Task BindingDataContext()
    {
        var textBlocks = new List<WeakReference>();
        WeakReference watcherReference;
        var data = new MySweetObject { Text = "Foo" };

        {
            var binding = new Binding("Text") { Mode = BindingMode.OneWay };
            var text = new TextBlock();
            textBlocks.Add(new WeakReference(text));
            text.DataContext = data;
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal(data.Text, text.Text);
            text.SetBinding(TextBlock.FontFamilyProperty, binding);
            Assert.Equal(data.Text, text.FontFamily.ToString());
            Assert.Single(data.Subscribers);

            text = new TextBlock();
            textBlocks.Add(new WeakReference(text));
            text.DataContext = data;
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal(data.Text, text.Text);
            text.SetBinding(TextBlock.FontFamilyProperty, binding);
            Assert.Equal(data.Text, text.FontFamily.ToString());

            // Single object subscribing
            Assert.Single(data.Subscribers);
            watcherReference = new WeakReference(data.Subscribers[0].Target);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        foreach (var textReference in textBlocks)
        {
            Assert.False(textReference.IsAlive, "TextBlock should not be alive!");
        }

        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // 1 Global watcher is still alive
        Assert.Single(data.Subscribers);
        var watcher = watcherReference.Target;
        Assert.NotNull(watcher);
        var currentManager = MyManager.GetCurrentManager(watcher.GetType());
        Assert.Same(currentManager, watcher);
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

    abstract class MyManager : WeakEventManager
    {
        public static new WeakEventManager GetCurrentManager(Type managerType) => WeakEventManager.GetCurrentManager(managerType);
    }
}