using Grpc.Core;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Provides client operations for user management functionality including authentication and user information retrieval.
    /// </summary>
    internal interface IUserManagementClient
    {
        /// <inheritdoc cref="UserManagementService.UserManagementServiceClient.LoginAsync" />
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <inheritdoc cref="UserManagementService.UserManagementServiceClient.LogoutAsync" />
        Task<LogoutResponse> LogoutAsync(LogoutRequest request);

        /// <inheritdoc cref="UserManagementService.UserManagementServiceClient.GetCurrentUserAsync" />
        Task<GetCurrentUserResponse> GetCurrentUserAsync(GetCurrentUserRequest request);

        /// <summary>
        /// Event invoked when an <see cref="RpcException"/> is caught during an RPC.
        /// </summary>
        event EventHandler<RpcErrorEventArgs> RpcError;
    }
}
