using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceInterface.Validation;

namespace DummyService
{
  public class Global : System.Web.HttpApplication
  {
    public class PeopleAppHost : AppHostBase
    {
      public PeopleAppHost() : base( "People Web Service", typeof( PeopleService ).Assembly ) { }

      public override void Configure( Funq.Container container )
      {
        SetConfig( new EndpointHostConfig { DebugMode = false } );

        Plugins.Add( new ValidationFeature() );

        container.Register<ICacheClient>( new MemoryCacheClient() );
        container.RegisterValidators( typeof( PeopleService ).Assembly );
      }
    }

    protected void Application_Start( object sender, EventArgs e )
    {
      new PeopleAppHost().Init();
    }

    protected void Session_Start( object sender, EventArgs e )
    {

    }

    protected void Application_BeginRequest( object sender, EventArgs e )
    {

    }

    protected void Application_AuthenticateRequest( object sender, EventArgs e )
    {

    }

    protected void Application_Error( object sender, EventArgs e )
    {

    }

    protected void Session_End( object sender, EventArgs e )
    {

    }

    protected void Application_End( object sender, EventArgs e )
    {

    }
  }
}