using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using DummyService;
using System.Net;
using System.Reactive.Linq;
using System.IO;
using System.Threading;
using System.Reactive.Concurrency;
using ServiceStack.Text;
using System.Diagnostics;

namespace WebServiceRxPoller
{

  public static class TimeSpanExtension
  {
    public static TimeSpan ExponentialInterval( this TimeSpan interval, double power = 2 )
    {
      return new TimeSpan(
        ( int ) Math.Pow( interval.Days, power ),
        ( int ) Math.Pow( interval.Hours, power ),
        ( int ) Math.Pow( interval.Minutes, power ),
        ( int ) Math.Pow( interval.Seconds, power ),
        ( int ) Math.Pow( interval.Milliseconds, power )
        );
    }
  }

  public static class ObservableExtension
  {
    public static IObservable<TSource> RetryWithBackOff<TSource, TException>(
      this IObservable<TSource> source,
      int retryLimit = 3,
      Func<int /* retries */, TimeSpan /* interval */> getInterval = null,
      Predicate<TException> canRetry = null,
      IScheduler scheduler = null
    )
      where TException : Exception
    {
      Func<int, TimeSpan> squaredInterval =
        ( retries ) => TimeSpan.FromSeconds( retries ).ExponentialInterval();
      getInterval = getInterval ?? squaredInterval;
      canRetry = canRetry == null ? ( error ) => true : canRetry;
      scheduler = scheduler ?? Scheduler.ThreadPool;

      Func<int, IObservable<TSource>> retry = null;
      retry = ( retries ) => source.Catch<TSource, TException>( error =>
      {
        if ( !canRetry( error ) || retries >= retryLimit )
          return Observable.Throw<TSource>( error );

        return Observable
          .Timer( getInterval( retries ), scheduler )
          .SelectMany( retry( retries + 1 ) );
      } );

      return retry( 0 );
    }
  }

  public static class WebRequestObservable
  {
    public static IObservable<WebResponse> AsDeferredObservable( Uri uri, Action<WebRequest> webRequestConfigurator )
    {
      return Observable.Defer( () =>
      {
        var request = WebRequest.Create( uri );
        webRequestConfigurator( request );
        return Observable.FromAsyncPattern<WebResponse>(
            request.BeginGetResponse, request.EndGetResponse
          )();
      } );
    }
  }

  class Program
  {
    static void Main( string[] args )
    {
      ITracer tracer = new ServiceStack.Text.Tracer.ConsoleTracer();
      Stopwatch stopWatch = new Stopwatch();

      var apiKey = Guid.NewGuid().ToString();
      var responseSource = WebRequestObservable.AsDeferredObservable(
        new Uri( "http://localhost/DummyService/people/" + apiKey ),
        ( webRequest ) =>
        {
          tracer.WriteDebug( "Retrying after {0} seconds...", stopWatch.ElapsedMilliseconds / 1000 );
          webRequest.Method = "GET";
          ( ( HttpWebRequest ) webRequest ).Accept = "application/json";
        } );

      var response = responseSource.RetryWithBackOff<WebResponse, WebException>(
        5,
        retries => TimeSpan.FromSeconds( retries ).ExponentialInterval(),
        exception => exception.IsNotFound(),
        Scheduler.NewThread
        );

      stopWatch.Start();
      IList<Person> people = null;
      response
        .Subscribe(
        r =>
        {
          people = JsonSerializer.DeserializeFromStream<Person[]>( r.GetResponseStream() );
        },
        Console.WriteLine,
        () =>
        {
          Console.WriteLine( "Completed." );
          foreach ( var person in people )
          {
            person.PrintDump();
          }
          stopWatch.Stop();
        }
      );

      Console.WriteLine( "Back to main thread. Simulating work by sleeping for 3 seconds..." );
      Thread.Sleep( 3000 );
      Console.WriteLine( "Main thread done." );

      Console.ReadKey();
    }
  }
}
