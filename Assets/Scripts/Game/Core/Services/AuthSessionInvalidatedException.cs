using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Raised when the current authenticated session is no longer valid and the player must sign in again.
    /// </summary>
    public sealed class AuthSessionInvalidatedException : Exception
    {
        public AuthSessionInvalidatedException(string message)
            : base(message)
        {
        }
    }
}
