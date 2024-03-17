using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(posTest.Startup))]
namespace posTest
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
