namespace NancyApplication
{
    using Microsoft.AspNet.Builder;
    using Nancy.Owin;
    using Db4objects.Db4o;
 
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(x => x.UseNancy());
        }
    }
}
