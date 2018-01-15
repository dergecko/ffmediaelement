﻿namespace Unosquare.FFME.Commands
{
    using Core;
    using System.Text;
    using Shared;

    /// <summary>
    /// Implements the logic to close a media stream.
    /// </summary>
    /// <seealso cref="Unosquare.FFME.Commands.MediaCommand" />
    internal sealed class CloseCommand : MediaCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseCommand" /> class.
        /// </summary>
        /// <param name="manager">The media element.</param>
        public CloseCommand(MediaCommandManager manager)
            : base(manager, MediaCommandType.Close)
        {
            // placeholder
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        internal override void ExecuteInternal()
        {
            var m = Manager.MediaCore;

            if (m.IsDisposed || m.IsOpen == false || m.IsOpening) return;

            m.Log(MediaLogMessageType.Debug, $"{nameof(CloseCommand)}: Entered");
            m.Clock.Pause();

            // Let the threads know a cancellation is pending.
            m.IsTaskCancellationPending = true;

            // Cause an immediate Packet read abort
            m.Container.SignalAbortReads(false);

            // Call close on all renderers
            foreach (var renderer in m.Renderers.Values)
                renderer.Close();

            // Stop all the workers
            m.StopWorkers();

            // Remove the renderers disposing of them
            m.Renderers.Clear();

            // Reset the clock
            m.Clock.Reset();

            // Dispose the container
            if (m.Container != null)
            {
                m.Container.Dispose();
                m.Container = null;
            }

            // Dispose the Blocks for all components
            foreach (var kvp in m.Blocks) kvp.Value.Dispose();
            m.Blocks.Clear();

            // Clear the render times
            m.LastRenderTime.Clear();
            m.MediaState = MediaEngineState.Close;
            m.SendOnMediaClosed();

            // Update notification properties
            m.ResetControllerProperties();
            m.ResetBufferingProperties();
            m.NotifyPropertyChanges();

            if (MediaEngine.Platform.IsInDebugMode)
            {
                if (RC.Current.InstancesByLocation.Count > 0)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Unmanaged references were left alive. This is an indication that there is a memory leak.");
                    foreach (var kvp in RC.Current.InstancesByLocation)
                        builder.AppendLine($"    {kvp.Key,30}: {kvp.Value}");

                    m.Log(MediaLogMessageType.Error, builder.ToString());
                }
            }

            m.Log(MediaLogMessageType.Debug, $"{nameof(CloseCommand)}: Completed");
        }
    }
}
