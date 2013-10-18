using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using DummyService;
using System.Net;
using System.Reactive.Linq;
using System.IO;

namespace WebServiceRxPoller
{
  class Program
  {
    static void Main( string[] args )
    {
      var apiKey = Guid.NewGuid().ToString();
      WebRequest peopleServiceRequest = HttpWebRequest.Create( "http://localhost/DummyService/people/" + apiKey );
      peopleServiceRequest.Method = "HEAD";
      ((HttpWebRequest) peopleServiceRequest).Accept = "application/xml";

      var response = from interval in Observable.Interval( TimeSpan.FromSeconds( 1 ) )
                       .Concat( Observable.Empty<long>().Delay( TimeSpan.FromSeconds( 2 ) ) )
                       .Timeout( TimeSpan.FromSeconds( 10 ) )
                     from r in Observable.FromAsyncPattern<WebResponse>(
                      peopleServiceRequest.BeginGetResponse, peopleServiceRequest.EndGetResponse
                     )()
                     //where ( ( HttpWebResponse ) r ).StatusCode == HttpStatusCode.OK
                     select r;

      response.Subscribe( 
        r =>
        {
          using ( StreamReader sr = new StreamReader( r.GetResponseStream() ) )
            Console.WriteLine( sr.ReadToEnd() );
        },
        e => Console.WriteLine( e.ToString() ),
        () => Console.WriteLine( "Completed." )
      );

      Console.WriteLine( "Timed out." );
      Console.ReadLine();
    }
  }
}
