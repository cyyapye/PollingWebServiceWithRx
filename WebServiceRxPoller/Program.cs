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

namespace WebServiceRxPoller
{
  class Program
  {
    public static readonly Func<int, TimeSpan> ExponentialBackoff = n => TimeSpan.FromSeconds( Math.Pow( n, 2 ) );

    public struct RetryTuple<T>
    {
      public bool CanRetry;
      public T Item;
      public Exception Exception;
    }

    static void Main( string[] args )
    {
      var apiKey = Guid.NewGuid().ToString();
      WebRequest peopleServiceRequest = HttpWebRequest.Create( "http://localhost/DummyService/people/" + apiKey );
      peopleServiceRequest.Method = "GET";
      ((HttpWebRequest) peopleServiceRequest).Accept = "application/xml";

      var responseSource = Observable.Defer( () =>
      {
        return Observable.FromAsyncPattern<WebResponse>(
          peopleServiceRequest.BeginGetResponse, peopleServiceRequest.EndGetResponse
          )();
      } );

      //var scheduler = new TestScheduler();
      int attempt = 0;
      var response = Observable.Defer( () =>
      {
        Console.WriteLine( "Attempt {0} at {1}", attempt, DateTime.Now.ToLongTimeString() );
        return ( ( ++attempt == 1 ) 
          ? responseSource 
          //: responseSource.Delay( ExponentialBackoff( attempt - 1 ), Scheduler.ThreadPool )
          : from i in Observable.Interval( ExponentialBackoff( attempt - 1 ) ).Timeout( ExponentialBackoff( attempt - 1 ) + TimeSpan.FromSeconds( 1 ) )
            from r in responseSource
            select r
          ).Select( r => new RetryTuple<WebResponse> { CanRetry = true, Item = r, Exception = null } )
          .Catch<RetryTuple<WebResponse>, Exception>( e =>
            // e.Status == WebExceptionStatus.ProtocolError && ( ( HttpWebResponse ) e.Response ).StatusCode == HttpStatusCode.NotFound
            e is WebException
            ? Observable.Throw<RetryTuple<WebResponse>>( e )
            : Observable.Return( new RetryTuple<WebResponse> { CanRetry = false, Item = default( WebResponse ), Exception = e } )
          );
      })
        .Retry( 6 )
        .SelectMany( r => r.CanRetry
          ? Observable.Return( r.Item )
          : Observable.Throw<WebResponse>( r.Exception )
        );

      //  return Observable.Defer( () =>
      //  {
      //    return ( ( ++attempt == 1 ) ? source : source.Delay( strategy( attempt - 1 ), scheduler ) )
      //        .Select( item => new Tuple<bool, T, Exception>( true, item, null ) )
      //        .Catch<Tuple<bool, T, Exception>, Exception>( e => retryOnError( e )
      //            ? Observable.Throw<Tuple<bool, T, Exception>>( e )
      //            : Observable.Return( new Tuple<bool, T, Exception>( false, default( T ), e ) ) );
      //  } )
      //  .Retry( retryCount )
      //  .SelectMany( t => t.Item1
      //      ? Observable.Return( t.Item2 )
      //      : Observable.Throw<T>( t.Item3 ) );

      //var response = from interval in Observable.Interval( TimeSpan.FromSeconds( 1 ) )
      //                 .Timeout( TimeSpan.FromSeconds( 6 ) )
      //               from r in responseSource()
      //               //where ( ( HttpWebResponse ) r ).StatusCode == HttpStatusCode.OK
      //               select r;

      response
        //.Catch<WebResponse, WebException>( e => response )
        //.OnErrorResumeNext(responseSource())
        .Subscribe(
        r =>
        {
          //if ( ( ( HttpWebResponse ) r ).StatusCode != HttpStatusCode.OK ) return;
          using ( StreamReader sr = new StreamReader( r.GetResponseStream() ) )
          {
            Console.WriteLine( sr.ReadToEnd() );
            Console.ReadLine();
          }
        },
        Console.WriteLine,
        () => { Console.WriteLine( "Completed." ); Console.ReadLine(); }
      );

      Console.WriteLine( "Back to main thread. Simulating work by sleeping for 8 seconds..." );
      Thread.Sleep( 8000 );
      Console.WriteLine( "Main thread done." );
      //Console.ReadLine();
    }


    // Licensed under the MIT license with <3 by GitHub

    /// <summary>
    /// An exponential back off strategy which starts with 1 second and then 4, 9, 16...
    /// </summary>

    //public static IObservable<T> RetryWithBackoffStrategy<T>(
    //    this IObservable<T> source,
    //    int retryCount = 3,
    //    Func<int, TimeSpan> strategy = null,
    //    Func<Exception, bool> retryOnError = null,
    //    IScheduler scheduler = null )
    //{
    //  strategy = strategy ?? ExponentialBackoff;
    //  scheduler = scheduler ?? Scheduler.ThreadPool;

    //  if ( retryOnError == null )
    //    retryOnError = e => e.CanRetry();

    //  int attempt = 0;

    //  return Observable.Defer( () =>
    //  {
    //    return ( ( ++attempt == 1 ) ? source : source.Delay( strategy( attempt - 1 ), scheduler ) )
    //        .Select( item => new Tuple<bool, T, Exception>( true, item, null ) )
    //        .Catch<Tuple<bool, T, Exception>, Exception>( e => retryOnError( e )
    //            ? Observable.Throw<Tuple<bool, T, Exception>>( e )
    //            : Observable.Return( new Tuple<bool, T, Exception>( false, default( T ), e ) ) );
    //  } )
    //  .Retry( retryCount )
    //  .SelectMany( t => t.Item1
    //      ? Observable.Return( t.Item2 )
    //      : Observable.Throw<T>( t.Item3 ) );
    //}
  }
}
