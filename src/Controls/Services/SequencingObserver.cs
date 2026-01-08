using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    internal sealed class SequencingObserver(
        ISequencingServiceClient sequencingService,
        IExecutionStateService executionStateService,
        ISequenceFileStateService sequenceFileStateService,
        ILogger<SequencingObserver> logger) : ISequencingObserver, IAsyncDisposable
    {
        public BlockingCollection<ObserveSequenceFileResponse> SequenceFileMessageCollection { get; set; } = [];
        public BlockingCollection<ObserveSequenceFileExecutionResponse> ExecutionMessageCollection { get; set; } = [];

        private CancellationTokenSource _sequenceFileObservingCTS = new();
        private CancellationTokenSource _sequenceFileMessageQueueProcessingCTS = new();
        private Task? _sequenceFileObservationTask;
        private Task? _sequenceFileMessageQueueProcessingTask;
        private readonly SemaphoreSlim _sequenceFileSemaphore = new(1, 1);
        private readonly object _sequenceFileCollectionLock = new();

        private readonly CancellationTokenSource _executionObservingCTS = new();
        private CancellationTokenSource _executionMessageQueueProcessingCTS = new();
        private Task? _executionObservationTask;
        private Task? _executionMessageQueueProcessingTask;
        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
        private readonly object _executionCollectionLock = new();

        public async Task BeginObservingSequenceFileAsync(string sequenceFileId)
        {
            ArgumentException.ThrowIfNullOrEmpty(sequenceFileId);
            await CancelSequenceFileObservationAsync();

            lock (_sequenceFileCollectionLock)
            {
                if (SequenceFileMessageCollection.IsAddingCompleted)
                {
                    SequenceFileMessageCollection.Dispose();
                    SequenceFileMessageCollection = [];
                }
            }
            _sequenceFileObservationTask = Task.Run(async () => await ObserveSequenceFileAsync(sequenceFileId));
        }

        private async Task CancelSequenceFileObservationAsync()
        {
            if (_sequenceFileObservationTask is not null)
            {
                await _sequenceFileObservingCTS.CancelAsync();
                try
                {
                    await _sequenceFileObservationTask;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Exception during sequence file observation task cancellation");
                }
                _sequenceFileObservationTask = null;
                _sequenceFileObservingCTS.Dispose();
                _sequenceFileObservingCTS = new CancellationTokenSource();
            }
        }

        private async Task ObserveSequenceFileAsync(string sequenceFileId)
        {
            try
            {
                logger.LogInformation("Started observing sequence file with ID = {Id}", sequenceFileId);

                ObserveSequenceFileRequest request = new() { SequenceFileId = sequenceFileId };
                await foreach (ObserveSequenceFileResponse responseMessageObject in sequencingService.ObserveSequenceFileAsync(request, _sequenceFileObservingCTS.Token))
                {
                    try
                    {
                        SequenceFileMessageCollection.Add(responseMessageObject, _sequenceFileObservingCTS.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (RpcException)
            {
                // any RPC exception here is either from the stream being intentionally cancelled,
                // or from the gRPC service becoming Unavailable, which will be reported on non-Observe RPCs.
            }
            finally
            {
                logger.LogInformation("Sequence File Observe stream closed for sequence file with ID {Id}", sequenceFileId);
            }
        }

        public async Task BeginObservingExecutionAsync()
        {
            if (_executionObservationTask is not null && !_executionObservationTask.IsCompleted)
            {
                return;
            }
            _executionObservationTask = Task.Run(ObserveSequenceFileExecutionAsync);
        }

        private async Task ObserveSequenceFileExecutionAsync()
        {
            try
            {
                logger.LogInformation("Started observing sequence file execution updates");

                ObserveSequenceFileExecutionRequest request = new() { ProcessId = Environment.ProcessId };
                await foreach (ObserveSequenceFileExecutionResponse responseMessageObject in sequencingService.ObserveSequenceFileExecutionAsync(request, _executionObservingCTS.Token))
                {
                    try
                    {
                        ExecutionMessageCollection.Add(responseMessageObject, _executionObservingCTS.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (RpcException)
            {
                // any RPC exception here is either from the stream being intentionally cancelled,
                // or from the gRPC service becoming Unavailable, which will be reported on non-Observe RPCs.
            }
            finally
            {
                logger.LogInformation("Sequence File Execution Observe stream has been closed");
            }
        }

        public bool IsProcessingSequenceFileMessageQueue { get; private set; }

        public bool IsProcessingExecutionMessageQueue { get; private set; }

        public async Task StartSequenceFileMessageProcessingAsync()
        {
            await StopSequenceFileMessageProcessingAsync();
            _sequenceFileMessageQueueProcessingTask = Task.Run(async () => await ProcessSequenceFileMessageQueueAsync(_sequenceFileMessageQueueProcessingCTS.Token));
            IsProcessingSequenceFileMessageQueue = true;
        }

        public async Task StopSequenceFileMessageProcessingAsync()
        {
            if (_sequenceFileMessageQueueProcessingTask is not null)
            {
                await _sequenceFileMessageQueueProcessingCTS.CancelAsync();
                try
                {
                    await _sequenceFileMessageQueueProcessingTask;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Exception during sequence file message queue processing task cancellation");
                }
                IsProcessingSequenceFileMessageQueue = false;
                _sequenceFileMessageQueueProcessingCTS.Dispose();
                _sequenceFileMessageQueueProcessingCTS = new CancellationTokenSource();
            }
        }

        public async Task StartExecutionMessageProcessingAsync()
        {
            await StopExecutionMessageProcessingAsync();
            _executionMessageQueueProcessingTask = Task.Run(async () => await ProcessExecutionMessageQueueAsync(_executionMessageQueueProcessingCTS.Token));
            IsProcessingExecutionMessageQueue = true;
        }

        public async Task StopExecutionMessageProcessingAsync()
        {
            if (_executionMessageQueueProcessingTask is not null)
            {
                await _executionMessageQueueProcessingCTS.CancelAsync();
                try
                {
                    await _executionMessageQueueProcessingTask;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Exception during execution message queue processing task cancellation");
                }
                IsProcessingExecutionMessageQueue = false;
                _executionMessageQueueProcessingCTS.Dispose();
                _executionMessageQueueProcessingCTS = new CancellationTokenSource();
            }
        }

        private async Task ProcessSequenceFileMessageQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (ObserveSequenceFileResponse message in SequenceFileMessageCollection.GetConsumingEnumerable(cancellationToken))
                {
                    await _sequenceFileSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await HandleSequenceFileMessageReceivedAsync(message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error processing observed message.");
                    }
                    finally
                    {
                        _ = _sequenceFileSemaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogDebug(e, "The sequence file message processing operation has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in processing sequence file messages.");
            }
        }

        private async Task ProcessExecutionMessageQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (ObserveSequenceFileExecutionResponse message in ExecutionMessageCollection.GetConsumingEnumerable(cancellationToken))
                {
                    await _executionSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await HandleExecutionMessageReceivedAsync(message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error processing observed execution message.");
                    }
                    finally
                    {
                        _ = _executionSemaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogDebug(e, "The sequence file execution message processing operation has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in processing execution messages.");
            }
        }

        private async Task HandleSequenceFileMessageReceivedAsync(ObserveSequenceFileResponse message)
        {
            switch (message?.UpdateCase)
            {
                case ObserveSequenceFileResponse.UpdateOneofCase.HeaderUpdate:
                    HandleHeaderUpdate(message.HeaderUpdate);
                    break;
                case ObserveSequenceFileResponse.UpdateOneofCase.ErrorUpdate:
                    HandleErrorUpdate(message.ErrorUpdate, executionId: null);
                    break;
                case ObserveSequenceFileResponse.UpdateOneofCase.SequenceFileUpdate:
                    HandleSequenceFileUpdate(message.SequenceFileUpdate);
                    break;
                case ObserveSequenceFileResponse.UpdateOneofCase.ProcessModelEntryPointsUpdate:
                    HandleProcessModelEntryPointsUpdate(message.ProcessModelEntryPointsUpdate);
                    break;
                case ObserveSequenceFileResponse.UpdateOneofCase.SelectionUpdate:
                case ObserveSequenceFileResponse.UpdateOneofCase.None:
                default:
                    break;
            }
        }

        private async Task HandleExecutionMessageReceivedAsync(ObserveSequenceFileExecutionResponse message)
        {
            switch (message?.UpdateCase)
            {
                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.ExecutionUpdate:
                    HandleExecutionUpdate(message.ExecutionId, message.ExecutionUpdate);
                    break;

                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.ExecutionStepListUpdate:
                    HandleExecutionStepListUpdate(message.ExecutionId, message.ExecutionStepListUpdate);
                    break;

                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.ErrorUpdate:
                    HandleErrorUpdate(message.ErrorUpdate, message.ExecutionId);
                    break;
                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.ExecutionResultUpdate:
                    HandleExecutionResultUpdate(message.ExecutionId, message.ExecutionResultUpdate);
                    break;
                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.ExecutionInfoUpdate:
                    HandleExecutionInfoUpdate(message.ExecutionId, message.ExecutionInfoUpdate);
                    break;
                case ObserveSequenceFileExecutionResponse.UpdateOneofCase.None:
                default:
                    break;
            }
        }

        private void HandleExecutionUpdate(int executionId, ExecutionUpdate? executionUpdate)
        {
            if (executionUpdate != null)
            {
                executionStateService.UpdateExecutionState(executionId, executionUpdate);
            }
        }

        private void HandleExecutionStepListUpdate(int executionId, ExecutionStepListUpdate? executionStepListUpdate)
        {
            if (executionStepListUpdate != null)
            {
                executionStateService.UpdateExecutionStepList(executionId, executionStepListUpdate);
            }
        }

        private void HandleSequenceFileUpdate(SequenceFileUpdate? sequenceFileUpdate)
        {
            if (sequenceFileUpdate != null)
            {
                sequenceFileStateService.UpdateSequenceFileState(sequenceFileUpdate);
            }
        }

        private void HandleHeaderUpdate(HeaderUpdate? headerUpdate)
        {
            if (headerUpdate != null)
            {
                sequenceFileStateService.UpdateHeaderState(headerUpdate);
            }
        }

        private void HandleProcessModelEntryPointsUpdate(ProcessModelEntryPointsUpdate processModelEntryPointsUpdate)
        {
            if (processModelEntryPointsUpdate != null)
            {
                sequenceFileStateService.UpdateProcessModelEntryPoints(processModelEntryPointsUpdate);
            }
        }

        private void HandleErrorUpdate(ErrorUpdate? errorUpdate, int? executionId = null)
        {
            if (errorUpdate != null)
            {
                if (executionId is not null)
                {
                    executionStateService.UpdateErrorState((int)executionId, errorUpdate);
                }
                else
                {
                    sequenceFileStateService.UpdateErrorState(errorUpdate);
                }
            }
        }

        private void HandleExecutionResultUpdate(int executionId, ExecutionResultUpdate? executionResultUpdate)
        {
            if (executionResultUpdate != null)
            {
                executionStateService.UpdateExecutionResult(executionId, executionResultUpdate);
            }
        }

        private void HandleExecutionInfoUpdate(int executionId, ExecutionInfoUpdate? executionInfoUpdate)
        {
            if (executionInfoUpdate != null)
            {
                executionStateService.UpdateExecutionInfoState(executionId, executionInfoUpdate);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _sequenceFileObservingCTS.CancelAsync();
            _sequenceFileObservingCTS.Dispose();
            await _sequenceFileMessageQueueProcessingCTS.CancelAsync();
            _sequenceFileMessageQueueProcessingCTS.Dispose();
            _sequenceFileSemaphore.Dispose();
            lock (_sequenceFileCollectionLock)
            {
                SequenceFileMessageCollection.Dispose();
            }

            await _executionObservingCTS.CancelAsync();
            _executionObservingCTS.Dispose();
            await _executionMessageQueueProcessingCTS.CancelAsync();
            _executionMessageQueueProcessingCTS.Dispose();
            _executionSemaphore.Dispose();
            lock (_executionCollectionLock)
            {
                ExecutionMessageCollection.Dispose();
            }
        }
    }
}
