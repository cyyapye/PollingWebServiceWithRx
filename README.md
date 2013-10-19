PollingWebServiceWithRx
=======================
A prototype for polling a web service with back off using Rx extensions.

## DummyService
A dummy people web service that serves up a pre-canned list of famous investors. The service simulates a delayed availability of data typically seen in asynchronous processing. Initially, the service will return a 404 when data is unavailable. Once data is available, the service returns 200 with the list of investors. This simulation is achieved by caching the trigger time to deliver the data using the ApiKey as cache key.

## WebServiceRxPoller
A console application illustrating the use of deferred observables, a retry mechanism with a back off strategy to achieve a fairly robust polling of the DummyService. The application starts by making a request to the DummyService. It then traps the resulting 404 error and retries based on an exponential time interval series. Since the DummyService simulates a successsful response after five seconds, the application stops polling on success and spits out the content. The application consists of the following convenient extensions.

### TimeSpanExtension.ExponentialInterval
Calculates and returns the exponential of a given TimeSpan interval.

### ObservableExtension.RetryWithBackOff
Heavily borrowed from Andreas Köpf [solution](http://social.msdn.microsoft.com/Forums/en-US/af43b14e-fb00-42d4-8fb1-5c45862f7796/recursive-async-web-requestresponse-what-is-best-practice-3rd-try?forum=rx), this has been enhanced to accept all optional parameters that override default behaviors:
* retryLimit - Default is 3.
* getInterval - A function that returns the interval to wait before the next retry.
* canRetry - A predicate to determine if you want to retry based on either the type of exception or other criteria.
* scheduler - The observers run on the ThreadPool scheduler by default. You may choose a more suitable scheduler such as the TestScheduler when writing tests.

### WebRequestObservable.Create
This is intended to enforce the creation of a cold observable on a new WebRequest with every retry. It is critical to ensure the WebRequest is created with every retry, otherwise reusing the same WebRequest results in the same response with every retry leading to exhaustion of retries up to the specified retryLimit. By taking control of the creation of the WebRequest, this eliminates the risk of mistakenly reusing the same WebRequest. The Create method takes in the following parameters:
* uri - Uri for the WebRequest.
* configure - An optional action to further configure the WebRequest before passing it on to the observer.

License
=======
MIT