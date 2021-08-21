# AsyncThreading
Async thread support for .NET

This package provides an AsyncThread with a syncronization context. Work can be queued onto the AsyncThread and the result awaited.
AsyncThreads can communicate with each other or any thread with a syncronization context using a Messenger, mesages are automatically handled on the subscribers thread.