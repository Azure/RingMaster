// <copyright file="AppDomainExtensions.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RSLValidation
{
    using System;
    using System.Threading;

    public static class AppDomainExtensions
    {
        public static void Terminate(this AppDomain appdomain, bool restart)
        {
            if (appdomain == null)
            {
                throw new ArgumentNullException(nameof(appdomain));
            }

            appdomain.SetData("_RestartAppDomain", restart);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(100);
                AppDomain.Unload(appdomain);
            });
        }

        public static bool NeedsRestart(this AppDomain appdomain)
        {
            if (appdomain == null)
            {
                throw new ArgumentNullException(nameof(appdomain));
            }

            bool restart = false;

            try
            {
                restart = (bool)appdomain.GetData("_RestartAppDomain");
            }
            catch (Exception)
            {
                // ignore
            }

            return restart;
        }
    }
}