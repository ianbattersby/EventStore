using System;
using EventStore.Core.Data;
using EventStore.Core.TransactionLog;
using EventStore.Core.TransactionLog.Checkpoint;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.LogRecords;
using NUnit.Framework;

namespace EventStore.Core.Tests.TransactionLog.Chunks
{
    [TestFixture]
    public class when_having_scavenged_tfchunk_with_original_last_record_removed: SpecificationWithDirectoryPerTestFixture
    {
        private TFChunkDb _db;
        private TFChunk _scavengedChunk;
        private LogRecord _rec1, _rec2, _rec3;
        private RecordWriteResult _res1, _res2, _res3;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            _db = new TFChunkDb(new TFChunkDbConfig(PathName,
                                                    new VersionedPatternFileNamingStrategy(PathName, "chunk-"),
                                                    16 * 1024,
                                                    0,
                                                    new InMemoryCheckpoint(),
                                                    new ICheckpoint[0]));
            _db.OpenVerifyAndClean();
            
            var chunk = _db.Manager.GetChunk(0);

            _rec1 = LogRecord.SingleWrite(0, Guid.NewGuid(), Guid.NewGuid(), "es1", ExpectedVersion.Any, "et1",
                                          new byte[] { 0, 1, 2 }, new byte[] { 5, 7 });
            _res1 = chunk.TryAppend(_rec1);

            _rec2 = LogRecord.SingleWrite(_res1.NewPosition, 
                                          Guid.NewGuid(), Guid.NewGuid(), "es1", ExpectedVersion.Any, "et1",
                                          new byte[] { 0, 1, 2 }, new byte[] { 5, 7 });
            _res2 = chunk.TryAppend(_rec2);
            
            _rec3 = LogRecord.SingleWrite(_res2.NewPosition,
                                          Guid.NewGuid(), Guid.NewGuid(), "es-to-scavenge", ExpectedVersion.Any, "et1",
                                          new byte[] { 0, 1, 2 }, new byte[] { 5, 7 });
            _res3 = chunk.TryAppend(_rec3);

            chunk.Complete();

            var scavenger = new TFChunkScavenger(_db, new FakeReadIndex(x => x == "es-to-scavenge"));
            scavenger.Scavenge(alwaysKeepScavenged: true);

            _scavengedChunk = _db.Manager.GetChunk(0);
        }

        public override void TestFixtureTearDown()
        {
            _db.Dispose();

            base.TestFixtureTearDown();
        }

        [Test]
        public void first_record_was_written()
        {
            Assert.IsTrue(_res1.Success);
            Assert.AreEqual(0, _res1.OldPosition);
            Assert.AreEqual(_rec1.GetSizeWithLengthPrefixAndSuffix(), _res1.NewPosition);
        }

        [Test]
        public void second_record_was_written()
        {
            Assert.IsTrue(_res2.Success);
            Assert.AreEqual(_res1.NewPosition, _res2.OldPosition);
            Assert.AreEqual(_res1.NewPosition + _rec2.GetSizeWithLengthPrefixAndSuffix(), _res2.NewPosition);
        }

        [Test]
        public void third_record_was_written()
        {
            Assert.IsTrue(_res3.Success);
            Assert.AreEqual(_res2.NewPosition, _res3.OldPosition);
            Assert.AreEqual(_res2.NewPosition + _rec3.GetSizeWithLengthPrefixAndSuffix(), _res3.NewPosition);
        }

        [Test]
        public void original_record1_can_be_read_at_position()
        {
            var res = _scavengedChunk.TryReadAt((int)_res1.OldPosition);
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec1, res.LogRecord);
        }

        [Test]
        public void original_record2_can_be_read_at_position()
        {
            var res = _scavengedChunk.TryReadAt((int)_res2.OldPosition);
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec2, res.LogRecord);
        }
        
        [Test]
        public void original_record3_cant_be_read_at_position()
        {
            var res = _scavengedChunk.TryReadAt((int)_res3.OldPosition);
            Assert.IsFalse(res.Success);
        }

        [Test]
        public void the_first_record_read_is_record1()
        {
            var res = _scavengedChunk.TryReadFirst();
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec1, res.LogRecord);
            Assert.AreEqual(_res1.NewPosition, res.NextPosition);
        }

        [Test]
        public void the_closest_forward_after_first_record_is_record2()
        {
            var res = _scavengedChunk.TryReadClosestForward((int)_res1.NewPosition);
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec2, res.LogRecord);
            Assert.AreEqual(_res2.NewPosition, res.NextPosition);
        }

        [Test]
        public void the_next_closest_forward_cant_be_read()
        {
            var res = _scavengedChunk.TryReadClosestForward((int)_res2.NewPosition);
            Assert.IsFalse(res.Success);
        }

        [Test]
        public void the_last_record_read_is_record2()
        {
            var res = _scavengedChunk.TryReadLast();
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec2, res.LogRecord);
            Assert.AreEqual(_res2.OldPosition, res.NextPosition);
        }

        [Test]
        public void the_closest_backward_after_last_record_is_record1()
        {
            var res = _scavengedChunk.TryReadClosestBackward((int)_res2.OldPosition);
            Assert.IsTrue(res.Success);
            Assert.AreEqual(_rec1, res.LogRecord);
            Assert.AreEqual(_res1.OldPosition, res.NextPosition);
        }

        [Test]
        public void the_next_closest_backward_cant_be_read()
        {
            var res = _scavengedChunk.TryReadClosestBackward((int)_res1.OldPosition);
            Assert.IsFalse(res.Success);
        }
    }
}