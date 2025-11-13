// Test file with Purity analyzer violations
// Place this in a directory and scan it to verify analyzers work

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Violations
{
    // PURITY003: Static mutable collection
    private static List<string> _staticList = new List<string>();

    // PURITY002: Sync-over-async
    public string GetDataSync()
    {
        var task = GetDataAsync();
        return task.Result; // Violation: blocking on async
    }

    // PURITY001: Await inside loop
    public async Task ProcessItems(List<int> items)
    {
        foreach (var item in items)
        {
            await ProcessItemAsync(item); // Violation: await in loop
        }
    }

    private async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return "data";
    }

    private async Task ProcessItemAsync(int item)
    {
        await Task.Delay(10);
    }
}

