using Grpc.Core;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.BlazorOI.UI.Services.Events;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    /// <summary>
    /// Make calls to the gRPC Sequencing Service(s).
    /// </summary>
    internal interface ISequencingServiceClient
    {
        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.OpenSequenceFileAsync" />
        Task<OpenSequenceFileResponse> OpenSequenceFileAsync(OpenSequenceFileRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.CloseSequenceFileAsync" />
        Task<CloseSequenceFileResponse> CloseSequenceFileAsync(CloseSequenceFileRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.ObserveSequenceFile" />
        /// <remarks>It is recommended to call <see cref="ISequencingObserver.BeginObservingSequenceFileAsync(string)" /> to handle the response stream
        /// instead of calling this method directly.</remarks>
        IAsyncEnumerable<ObserveSequenceFileResponse> ObserveSequenceFileAsync(ObserveSequenceFileRequest request, CancellationToken token);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.ObserveSequenceFileExecution" />
        /// <remarks>It is recommended to call <see cref="ISequencingObserver.BeginObservingExecutionAsync()" /> to handle the response stream
        /// instead of calling this method directly.</remarks>
        IAsyncEnumerable<ObserveSequenceFileExecutionResponse> ObserveSequenceFileExecutionAsync(ObserveSequenceFileExecutionRequest request, CancellationToken token);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.AdvancedExecuteSequenceFileAsync" />
        Task<AdvancedExecuteSequenceFileResponse> AdvancedExecuteSequenceFileAsync(AdvancedExecuteSequenceFileRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.AdvancedUpdateExecutionAsync" />
        Task<AdvancedUpdateExecutionResponse> AdvancedUpdateExecutionAsync(AdvancedUpdateExecutionRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.RestartExecutionAsync"/>
        Task<RestartExecutionResponse> RestartExecutionAsync(RestartExecutionRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.CreateOrUpdateBreakpointAsync" />
        Task<CreateOrUpdateBreakpointResponse> CreateOrUpdateBreakpointAsync(CreateOrUpdateBreakpointRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.DeleteBreakpointAsync" />
        Task<DeleteBreakpointResponse> DeleteBreakpointAsync(DeleteBreakpointRequest request);

        /// <inheritdoc cref="AdvancedSequencingService.AdvancedSequencingServiceClient.CloseExecutionAsync" />
        Task<CloseExecutionResponse> CloseExecutionAsync(CloseExecutionRequest request);

        /// <summary>
        /// Event invoked when an <see cref="RpcException"/> is caught during an RPC.
        /// </summary>
        event EventHandler<RpcErrorEventArgs> RpcError;
    }
}
