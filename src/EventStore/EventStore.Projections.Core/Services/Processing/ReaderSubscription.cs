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
using EventStore.Projections.Core.Messages;

namespace EventStore.Projections.Core.Services.Processing
{
    public class ReaderSubscription : ReaderSubscriptionBase, IReaderSubscription
    {
        public ReaderSubscription(
            IPublisher publisher, Guid subscriptionId, CheckpointTag @from, IReaderStrategy readerStrategy,
            long? checkpointUnhandledBytesThreshold, int? checkpointProcessedEventsThreshold, bool stopOnEof = false,
            int? stopAfterNEvents = null)
            : base(
                publisher, subscriptionId, @from, readerStrategy, checkpointUnhandledBytesThreshold,
                checkpointProcessedEventsThreshold, stopOnEof, stopAfterNEvents)
        {
        }

        public void Handle(ReaderSubscriptionMessage.CommittedEventDistributed message)
        {
            ProcessOne(message);
        }

        public void Handle(ReaderSubscriptionMessage.EventReaderIdle message)
        {
            // ignore
        }

        public void Handle(ReaderSubscriptionMessage.EventReaderPartitionDeleted message)
        {
            if (!base._eventFilter.DeletedNotificationPasses(message.PositionStreamId))
                return;
            var deletePosition = _positionTagger.MakeCheckpointTag(_positionTracker.LastTag, message);
            PublishPartitionDeleted(message.Partition, deletePosition);
        }
    }
}
