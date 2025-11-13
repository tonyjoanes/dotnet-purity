### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
PURITY001 | Purity | Warning | Detects await expressions inside loop constructs.
PURITY002 | Purity | Warning | Flags synchronous waits on asynchronous operations.
PURITY003 | Purity | Warning | Detects static mutable collection fields that leak shared state.
PURITY004 | Purity | Warning | Detects IDisposable instances that are not properly disposed.
PURITY005 | Purity | Warning | Detects event handler subscriptions that may cause memory leaks.
PURITY006 | Purity | Warning | Detects IEnumerable instances that are enumerated multiple times.
PURITY007 | Purity | Warning | Detects potential null reference dereferences without null checks.
PURITY008 | Purity | Warning | Detects exceptions that are caught but not logged, handled, or rethrown.
PURITY009 | Purity | Warning | Detects string concatenation using + or += within loops.
PURITY010 | Purity | Warning | Detects use of insecure cryptographic algorithms (MD5, SHA1, DES, etc.).
PURITY013 | Purity | Warning | Detects shared mutable state accessed without proper synchronization.


