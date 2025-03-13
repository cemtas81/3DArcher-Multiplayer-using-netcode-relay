using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.Tools.Samples.NetworkScenario
{
    public class LagSpikeScenarioWithAsyncTask : NetworkScenarioTask
    {
        [FormerlySerializedAs("m_DurationBetweenLagSpikesMilliseconds")]
        [SerializeField]
        int m_LagSpikeIntervalMs;

        [FormerlySerializedAs("m_LagSpikeDurationMilliseconds")]
        [SerializeField]
        int m_LagSpikeDurationMs;

        protected override async Task Run(INetworkEventsApi networkEventsApi, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(m_LagSpikeIntervalMs), cancellationToken);

                // Make sure to check if the user paused the scenario. This isn't pausing all waiting behavior
                // but will skip the lag spike, which is essentially the behavior we want to avoid when paused.
                // In case the lag spike duration is 0, we yield immediately as there is no need to trigger a lag spike.
                if (IsPaused || m_LagSpikeDurationMs == 0)
                {
                    await Task.Yield();
                    continue;
                }

                await networkEventsApi.TriggerLagSpikeAsync(TimeSpan.FromMilliseconds(m_LagSpikeDurationMs));
            }
        }
    }
}
