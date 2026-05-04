using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Game.Core.Services;
using Zenject;

namespace Game.Unity.Installers
{
    /// <summary>
    /// Warms up auth session restoration during startup so later flows can depend on a hydrated service.
    /// </summary>
    public sealed class AuthLifetime : IInitializable
    {
        private readonly IAuthService authService_;
        private readonly IGameLogger logger_;

        public AuthLifetime(IAuthService authService, IGameLogger logger)
        {
            authService_ = authService;
            logger_ = logger;
        }

        public void Initialize()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await authService_.InitializeAsync();
            }
            catch (Exception exception)
            {
                logger_?.Log($"[Auth] Failed to initialize auth service: {exception}");
            }
        }
    }
}
