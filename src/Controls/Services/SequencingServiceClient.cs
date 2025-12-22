using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;
using NationalInstruments.TestStand.WebOI.Utilities.Discovery;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    internal sealed class SequencingServiceClient(
        ILogger<SequencingServiceClient> logger,
        IDiscoveryServiceUtility serviceUtility) : ISequencingServiceClient, IUserManagementClient, ISequencingServiceClientConnection
    {
        private AdvancedSequencingService.AdvancedSequencingServiceClient? _client;
        private UserManagementService.UserManagementServiceClient? _userManagementServiceClient;
        private GrpcChannel? _grpcChannel;
        private static readonly GrpcChannelOptions _grpcChannelOptions = new()
        {
            ServiceConfig = new ServiceConfig()
            {
                MethodConfigs =
                {
                    new MethodConfig
                    {
                        Names = { MethodName.Default },
                        RetryPolicy = new RetryPolicy
                        {
                            MaxAttempts = 5,
                            InitialBackoff = TimeSpan.FromSeconds(1),
                            MaxBackoff = TimeSpan.FromSeconds(5),
                            BackoffMultiplier = 1.5,
                            RetryableStatusCodes = { StatusCode.Unavailable }
                        }
                    }
                }
            }
        };

        private ConnectionStatus _connectionStatus;

        public async Task<OpenSequenceFileResponse> OpenSequenceFileAsync(OpenSequenceFileRequest request)
        {
            request.ProcessId = Environment.ProcessId;
            return await CallRpcAsync(_client!.OpenSequenceFileAsync, request);
        }

        public async Task<CloseSequenceFileResponse> CloseSequenceFileAsync(CloseSequenceFileRequest request)
        {
            return await CallRpcAsync(_client!.CloseSequenceFileAsync, request);
        }

        public async Task<AdvancedExecuteSequenceFileResponse> AdvancedExecuteSequenceFileAsync(AdvancedExecuteSequenceFileRequest request)
        {
            return await CallRpcAsync(_client!.AdvancedExecuteSequenceFileAsync, request);
        }

        public async Task<AdvancedUpdateExecutionResponse> AdvancedUpdateExecutionAsync(AdvancedUpdateExecutionRequest request)
        {
            return await CallRpcAsync(_client!.AdvancedUpdateExecutionAsync, request);
        }

        public async Task<CreateOrUpdateBreakpointResponse> CreateOrUpdateBreakpointAsync(CreateOrUpdateBreakpointRequest request)
        {
            try
            {
                return await _client!.CreateOrUpdateBreakpointAsync(request);
            }
            catch (RpcException e)
            {
                HandleRpcException(nameof(CreateOrUpdateBreakpointAsync), e);
                throw;
            }
        }

        public async Task<DeleteBreakpointResponse> DeleteBreakpointAsync(DeleteBreakpointRequest request)
        {
            try
            {
                return await _client!.DeleteBreakpointAsync(request);
            }
            catch (RpcException e)
            {
                HandleRpcException(nameof(DeleteBreakpointAsync), e);
                throw;
            }
        }

        public async IAsyncEnumerable<ObserveSequenceFileResponse> ObserveSequenceFileAsync(ObserveSequenceFileRequest request, [EnumeratorCancellation] CancellationToken token = default)
        {
            AsyncServerStreamingCall<ObserveSequenceFileResponse>? call = null;
            try
            {
                call = _client!.ObserveSequenceFile(request, cancellationToken: token);
                _ = await call.ResponseHeadersAsync;
            }
            catch (RpcException e)
            {
                HandleRpcException(nameof(ObserveSequenceFileAsync), e);
                yield break;
            }
            while (await call.ResponseStream.MoveNext())
            {
                ObserveSequenceFileResponse current = call.ResponseStream.Current;
                logger.LogInformation("Observed response for sequence file ID {Id}: {Response}", request.SequenceFileId, current);
                if (current != null)
                {
                    yield return current;
                }
            }
            call.Dispose();
        }

        public async IAsyncEnumerable<ObserveSequenceFileExecutionResponse> ObserveSequenceFileExecutionAsync(ObserveSequenceFileExecutionRequest request, [EnumeratorCancellation] CancellationToken token = default)
        {
            AsyncServerStreamingCall<ObserveSequenceFileExecutionResponse>? call = null;
            try
            {
                call = _client!.ObserveSequenceFileExecution(request, cancellationToken: token);
                _ = await call.ResponseHeadersAsync;
            }
            catch (RpcException e)
            {
                HandleRpcException(nameof(ObserveSequenceFileExecutionAsync), e);
                yield break;
            }
            while (await call.ResponseStream.MoveNext())
            {
                ObserveSequenceFileExecutionResponse current = call.ResponseStream.Current;
                logger.LogInformation("Observed execution response with ID {Id}: {Response}", current.ExecutionId, current);
                if (current != null)
                {
                    yield return current;
                }
            }
            call.Dispose();
        }

        private async Task<TResponse> CallRpcAsync<TRequest, TResponse>(
            Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> rpc,
            TRequest request,
            [CallerMemberName] string methodName = "") where TResponse : new()
        {
            try
            {
                logger.LogInformation("Call to RPC {Method}: {Request}", methodName, request);
                return await rpc(request, null, null, default);
            }
            catch (RpcException e)
            {
                HandleRpcException(methodName, e);
            }
            return new TResponse();
        }

        private void HandleRpcException(string methodName, RpcException e)
        {
            if (e.StatusCode is StatusCode.Cancelled)
            {
                logger.LogInformation("RPC {MethodName} was cancelled. Message: {Message}.", methodName, e.Message);
                return;
            }
            logger.LogError("RPC {MethodName} failed. Message: {Message}.", methodName, e.Message);
            RpcError?.Invoke(this, new RpcErrorEventArgs(e));
        }

        public event EventHandler<RpcErrorEventArgs>? RpcError;

        #region ISequencingServiceClientConnection Members

        public event EventHandler? OnConnectionStatusChanged;

        public ConnectionStatus Status
        {
            get => _connectionStatus;
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    logger.LogInformation("Connection status changed to {Status}.", value);
                }
                OnConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public async Task ConnectServicesAsync()
        {
            try
            {
                Status = ConnectionStatus.Connecting;
                string? address = await serviceUtility.ResolveServiceAsync(ServiceConstants.SequencingServiceProvidedInterface);
                if (string.IsNullOrEmpty(address))
                {
                    throw new ArgumentException("Failed to resolve Sequencing Service.");
                }

                _grpcChannel = GrpcChannel.ForAddress(address, _grpcChannelOptions);
                AdvancedSequencingService.AdvancedSequencingServiceClient client = new(_grpcChannel);
                UserManagementService.UserManagementServiceClient userManagementClient = new(_grpcChannel);
                SetServices(client, userManagementClient);
                Status = ConnectionStatus.Connected;
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Connection to the sequencing service failed. Message: {Message}. Call Stack: {Stack}",
                    e.Message,
                    e.StackTrace);
                DisconnectServices();
                throw;
            }
        }

        private void SetServices(AdvancedSequencingService.AdvancedSequencingServiceClient sequencingClient, UserManagementService.UserManagementServiceClient userManagementClient)
        {
            if (_client is not null || _userManagementServiceClient is not null)
            {
                string message = $"Attempted to call {nameof(ConnectServicesAsync)} before calling {nameof(DisconnectServices)}.";
                logger.LogError("{Message}", message);
                throw new InvalidOperationException(message);
            }

            _client = sequencingClient;
            _userManagementServiceClient = userManagementClient;
            logger.LogInformation("Successfully connected services to {Service}.", nameof(SequencingServiceClient));
        }

        /// <inheritdoc />
        public void DisconnectServices()
        {
            try
            {
                _client = null;
                _userManagementServiceClient = null;
                _grpcChannel?.Dispose();
                logger.LogInformation("Successfully disconnected services from {Service}.", nameof(SequencingServiceClient));
                Status = ConnectionStatus.Disconnected;
            }
            catch (Exception e)
            {
                logger.LogError("Disconnect of services failed. Message: {Message}.", e.Message);
                throw;
            }
        }

        #endregion ISequencingServiceClientConnection Members

        #region UserManagement Members

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // We will be displaying the error message within the login dialog itself if the status code is unauthenticted and not in the application error banner.
            // Hence calling the Rpc directly here, without the CallRpcAsync wrapper which raises the RpcError event.
            try
            {
                return await _userManagementServiceClient!.LoginAsync(request);
            }
            catch (RpcException ex) when (ex.StatusCode != StatusCode.Unauthenticated)
            {
                HandleRpcException(nameof(LoginAsync), ex);
            }
            catch (RpcException)
            {
            }
            return new LoginResponse();
        }

        public async Task<LogoutResponse> LogoutAsync(LogoutRequest request)
        {
            return await CallRpcAsync(_userManagementServiceClient!.LogoutAsync, request);
        }

        public async Task<GetCurrentUserResponse> GetCurrentUserAsync(GetCurrentUserRequest request)
        {
            return await CallRpcAsync(_userManagementServiceClient!.GetCurrentUserAsync, request);
        }

        #endregion UserManagement Members
    }
}