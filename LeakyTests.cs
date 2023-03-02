using System.Windows.Controls;
using System.Windows.Data;

namespace wpf_memory_leaks;

public class LeakyTests
{
    [Fact]
    public async Task Test1()
    {
        WeakReference reference;
        Binding binding = new Binding("Text");
        binding.Source = new { Text = "Foo" };

        {
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
}