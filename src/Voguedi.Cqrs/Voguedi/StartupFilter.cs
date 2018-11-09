using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Voguedi
{
    class StartupFilter : IStartupFilter
    {
        #region IStartupFilter

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseVoguedi();
                next?.Invoke(app);
            };
        }

        #endregion
    }
}
