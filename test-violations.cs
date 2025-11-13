// Test file with Purity analyzer violations
// Place this in a directory and scan it to verify analyzers work

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // PURITY004: IDisposable not disposed
    public void ReadFileWithoutDispose()
    {
        var reader = new StreamReader("file.txt"); // Violation: StreamReader not disposed
        var content = reader.ReadToEnd();
        // Missing: reader.Dispose() or using statement
    }

    public void CreateMemoryStream()
    {
        var stream = new MemoryStream(); // Violation: MemoryStream should be disposed
        stream.WriteByte(1);
        // Should use: using var stream = new MemoryStream();
    }

    // PURITY005: Event handler leak
    public class EventPublisher
    {
        public event EventHandler? SomethingHappened;
    }

    public void SubscribeToEvent(EventPublisher publisher)
    {
        publisher.SomethingHappened += OnSomethingHappened; // Violation: never unsubscribed
        // Should unsubscribe in Dispose method or when no longer needed
    }

    private void OnSomethingHappened(object? sender, EventArgs e)
    {
        // Handler implementation
    }

    // PURITY006: Multiple enumeration of IEnumerable
    public void EnumerateMultipleTimes(IEnumerable<int> numbers)
    {
        var count = numbers.Count(); // First enumeration
        var sum = numbers.Sum(); // Second enumeration - Violation!
        var first = numbers.First(); // Third enumeration - Violation!
        
        // Should materialize first: var list = numbers.ToList();
    }

    public void ProcessSequence(IEnumerable<string> items)
    {
        if (items.Any()) // First enumeration
        {
            foreach (var item in items) // Second enumeration - Violation!
            {
                Console.WriteLine(item);
            }
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

// Additional test cases

public class DisposableResource : IDisposable
{
    public void Dispose()
    {
        // Dispose implementation
    }
}

public class ResourceUser
{
    // PURITY004: Disposable resource not disposed
    public void UseResource()
    {
        var resource = new DisposableResource(); // Violation: not disposed
        resource.ToString();
    }

    // Correct usage (should not trigger violation)
    public void UseResourceCorrectly()
    {
        using var resource = new DisposableResource();
        resource.ToString();
    }
}

public class EventSubscriber : IDisposable
{
    private EventPublisher? _publisher;

    // PURITY005: Event subscription without unsubscribe in Dispose
    public void Subscribe(EventPublisher publisher)
    {
        _publisher = publisher;
        publisher.SomethingHappened += HandleEvent; // Violation: should unsubscribe in Dispose
    }

    public void Dispose()
    {
        // Missing: _publisher.SomethingHappened -= HandleEvent;
    }

    private void HandleEvent(object? sender, EventArgs e)
    {
    }
}

// Additional violations for new analyzers

public class NullReferenceViolations
{
    // PURITY007: Null reference dereference
    public void ProcessData(string? data)
    {
        var length = data.Length; // Violation: data may be null
        Console.WriteLine(length);
    }

    public void AccessProperty(object? obj)
    {
        var hash = obj.GetHashCode(); // Violation: obj may be null
    }

    public void ArrayAccess(int[]? array)
    {
        var first = array[0]; // Violation: array may be null
    }
}

public class ExceptionHandlingViolations
{
    // PURITY008: Swallowed exception
    public void SwallowException()
    {
        try
        {
            RiskyOperation();
        }
        catch (Exception)
        {
            // Violation: exception caught but not logged or rethrown
        }
    }

    public void SwallowWithEmptyBlock()
    {
        try
        {
            RiskyOperation();
        }
        catch (Exception ex)
        {
            // Violation: exception caught but only commented, not handled
        }
    }

    private void RiskyOperation()
    {
        throw new InvalidOperationException("Test");
    }
}

public class StringConcatenationViolations
{
    // PURITY009: String concatenation in loop
    public string BuildString(List<string> items)
    {
        string result = "";
        foreach (var item in items)
        {
            result += item; // Violation: string concatenation in loop
        }
        return result;
    }

    public string CombineStrings(string[] parts)
    {
        string combined = "";
        for (int i = 0; i < parts.Length; i++)
        {
            combined = combined + parts[i]; // Violation: string concatenation in loop
        }
        return combined;
    }
}

public class CryptographicViolations
{
    // PURITY010: Insecure algorithm
    public void UseInsecureHash()
    {
        var md5 = System.Security.Cryptography.MD5.Create(); // Violation: MD5 is insecure
        var sha1 = System.Security.Cryptography.SHA1.Create(); // Violation: SHA1 is insecure
    }

    public void UseInsecureEncryption()
    {
        var des = new System.Security.Cryptography.DESCryptoServiceProvider(); // Violation: DES is insecure
        var rc2 = new System.Security.Cryptography.RC2CryptoServiceProvider(); // Violation: RC2 is insecure
    }
}

public class ThreadSafetyViolations
{
    // PURITY013: Thread safety violation
    private static List<string> _sharedList = new List<string>(); // Shared mutable state

    public void UnsafeAdd(string item)
    {
        _sharedList.Add(item); // Violation: modifying shared state without synchronization
    }

    public void UnsafeRemove(string item)
    {
        _sharedList.Remove(item); // Violation: modifying shared state without synchronization
    }

    private static Dictionary<string, int> _sharedDictionary = new Dictionary<string, int>();

    public void UnsafeUpdate(string key, int value)
    {
        _sharedDictionary[key] = value; // Violation: updating shared state without lock
    }
}
