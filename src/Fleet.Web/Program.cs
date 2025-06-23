// Sleepr.Web/Program.cs
using Fleet.Web;
using WebHostBuilder = Fleet.Web.WebHostBuilder;

WebHostBuilder
    .MyCreateHostBuilder(args)
    .Build()
    .Run();