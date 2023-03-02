using System.Windows.Controls;
using System.Windows.Data;

namespace wpf_memory_leaks;

public class LeakyTests
{
    [UIFact]
    public async Task BindingSource()
    {
        WeakReference reference;
        var data = new { Text = "Foo" };

        {
            var binding = new Binding("Text") { Source = data };
            var text = new TextBlock();
            reference = new WeakReference(text);
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(reference.IsAlive, "TextBlock should not be alive!");
    }

    [UIFact]
    public async Task BindingDataContext()
    {
        WeakReference reference;
        var data = new { Text = "Foo" };

        {
            var binding = new Binding("Text") { Mode = BindingMode.OneWay };
            var text = new TextBlock();
            reference = new WeakReference(text);
            text.DataContext = data;
            text.SetBinding(TextBlock.TextProperty, binding);
            Assert.Equal("Foo", text.Text);
        }
        
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.False(reference.IsAlive, "TextBlock should not be alive!");
    }
}