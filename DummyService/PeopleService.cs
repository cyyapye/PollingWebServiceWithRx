using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.FluentValidation;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using System.Net;

namespace DummyService
{
  public class Person : IReturn<List<Person>>
  {
    public string Name { get; set; }
  }

  [Route( "/people" )]
  [Route( "/people/{ApiKey}", "GET" )]
  public class GetPeople : IReturn<List<Person>>
  {
    public string ApiKey { get; set; }
  }

  public class PeopleValidator : AbstractValidator<GetPeople>
  {
    public PeopleValidator()
    {
      RuleFor( x => x.ApiKey ).NotEmpty().WithErrorCode( "ShouldNotBeEmpty" );
    }
  }
  
  public class PeopleService : Service
  {
    private static readonly Person[] EmptyPeopleList = new Person[ 0 ];
    private static readonly Person[] Investors = new[] {
          new Person { Name = "Warren Buffett" },
          new Person { Name = "Charles Munger" },
          new Person { Name = "Benjamin Graham" }
        };

    public object Get( GetPeople request )
    {
      var returnResultsAtUrn = UrnId.Create( "returnResultsAt", request.ApiKey );
      var returnResultsAt = Cache.Get<DateTime?>( returnResultsAtUrn );
      if ( returnResultsAt == null )
      {
        returnResultsAt = DateTime.Now.AddSeconds( 5 );
        Cache.Set( returnResultsAtUrn, returnResultsAt, DateTime.Now.AddHours( 1 ) );
      }

      if ( returnResultsAt >= DateTime.Now )
      {
        throw HttpError.NotFound( "The resource requested cannot be found." );
      }

      return Investors.AsEnumerable();
    }
  }
}