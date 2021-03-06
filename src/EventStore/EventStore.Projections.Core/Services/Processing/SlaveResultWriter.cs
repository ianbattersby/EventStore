// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using EventStore.Core.Bus;
using EventStore.Projections.Core.Messages.ParallelQueryProcessingMessages;

namespace EventStore.Projections.Core.Services.Processing
{
    public class SlaveResultWriter : IResultWriter
    {
        private readonly IPublisher _resultsPublisher;
        private readonly Guid _masterCoreProjectionId;

        public SlaveResultWriter(IPublisher resultsPublisher, Guid masterCoreProjectionId)
        {
            if (resultsPublisher == null) throw new ArgumentNullException("resultsPublisher");

            _resultsPublisher = resultsPublisher;
            _masterCoreProjectionId = masterCoreProjectionId;
        }

        public void WriteEofResult(
            Guid subscriptionId, string partition, string resultBody, CheckpointTag causedBy, Guid causedByGuid,
            string correlationId)
        {
            _resultsPublisher.Publish(
                new PartitionProcessingResult(
                    _masterCoreProjectionId, subscriptionId, partition, causedByGuid, causedBy, resultBody));
        }

        public void WritePartitionMeasured(Guid subscriptionId, string partition, int size)
        {
            _resultsPublisher.Publish(new PartitionMeasured(_masterCoreProjectionId, subscriptionId, partition, size));
        }

        public void WriteRunningResult(EventProcessedResult result)
        {
            // intentionally does nothing            
        }

        public void AccountPartition(EventProcessedResult result)
        {
            // intentionally does nothing            
        }

        public void EventsEmitted(
            EmittedEventEnvelope[] scheduledWrites, Guid causedBy, string correlationId)
        {
            throw new NotSupportedException();
        }

        public void WriteProgress(Guid subscriptionId, float progress)
        {
            _resultsPublisher.Publish(
                new PartitionProcessingProgress(_masterCoreProjectionId, subscriptionId, progress));
        }
    }
}
