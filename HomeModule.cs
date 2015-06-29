namespace NancyApplication
{
    using Nancy;
    using TreeDb4o;
    
    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ => "Hello World";
        }
    }
}
