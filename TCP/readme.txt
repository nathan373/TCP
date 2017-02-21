TCPClient is a client simulator to test multiple request (iterations) with multiple client (clientCount).

TCPService includes Communication Library, Window Service "TCPWinService", Console Server "TestServer" and unit test.

"TestServer" & "SocketClient" can be use as testing purpose. 

==================================================Reason==============================================================
SocketAsyncEventArgs uses I/O Completion Ports via the asynchronous methods in the .NET Socket class. I/O Completion Ports (IOCP) has two facets. It first allows I/O handles like file handles, socket handles, etc., to be associated with a completion port. Any async I/O completion event related to the I/O handle associated with the IOCP will get queued onto this completion port. This allows threads to wait on the IOCP for any completion events. The second facet is that we can create a I/O completion port that is not associated with any I/O handle. In this case, the IOCP is purely used as a mechanism for efficiently providing a thread-safe waitable queue technique. This technique is interesting and efficient. Using this technique, a pool of a few threads can achieve good scalability and performance for an application. 

==================================================Trade Off==============================================================
Consider using a fixed number of threads (2-8). Either load balance the socket connections across these threads, or just use a work pool of threads to service request messages parsed off the socket thread. 
TLS/SSL support.