//
// DiskWriterTests.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2009 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Client.PieceWriters;
using MonoTorrent.Common;
using NUnit.Framework;
using System.IO;
using System.Threading;

namespace MonoTorrent.Client
{
    public class ExceptionWriter : PieceWriter
    {
        public bool exist, close, flush, move, read, write;

        public override bool Exists(TorrentFile file)
        {
            if (exist)
                throw new Exception("exists");
            return true;
        }

        public override void Close(TorrentFile file)
        {
            if (close)
                throw new Exception("close");
        }

        public override void Flush(TorrentFile file)
        {
            if (flush)
                throw new Exception("flush");
        }

        public override void Move(string oldPath, string newPath, bool ignoreExisting)
        {
            if (move)
                throw new Exception("move");
        }

        public override int Read(TorrentFile file, long offset, byte[] buffer, int bufferOffset, int count)
        {
            if (read)
                throw new Exception("read");
            return count;
        }

        public override void Write(TorrentFile file, long offset, byte[] buffer, int bufferOffset, int count)
        {
            if (write)
                throw new Exception("write");
        }
    }

    [TestFixture]
    public class DiskWriterTests
    {
        byte [] data = new byte [Piece.BlockSize];
        DiskManager diskManager;
        ManualResetEvent handle;
        TestRig rig;
        ExceptionWriter writer;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            rig = TestRig.CreateMultiFile();
            diskManager = rig.Engine.DiskManager;
        }

        [SetUp]
        public void Setup()
        {
            writer = new ExceptionWriter();
            diskManager.Writer = writer;
            handle = new ManualResetEvent(false);
            rig.Manager.Stop();
        }

        [TearDown]
        public void Teardown()
        {
            handle.Close();
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            rig.Dispose();
        }

        [Test]
        public void CloseFail()
        {
            writer.close = true;
            Hookup();
            diskManager.CloseFilesAsync (rig.Manager).Wait();
            CheckFail();
        }

        [Test]
        public void FlushFail()
        {
            writer.flush = true;
            Hookup();
            diskManager.FlushAsync(rig.Manager, 0).Wait();
            CheckFail();
        }

        [Test]
        public void MoveFail()
        {
            writer.move = true;
            Hookup();
            diskManager.MoveFilesAsync(rig.Manager, "root", true).Wait();
            CheckFail();
        }

        [Test]
        public void ReadFail()
        {
            writer.read = true;
            Hookup();
            diskManager.ReadAsync (rig.Manager, 0, data, data.Length).Wait ();
            CheckFail();
        }

        [Test]
        public void WriteFail()
        {
            writer.write = true;
            Hookup();
            diskManager.WriteAsync(rig.Manager, 0, data, data.Length).Wait ();
            CheckFail();
        }

        void Hookup()
        {
            rig.Manager.TorrentStateChanged += delegate {
                if (rig.Manager.State == TorrentState.Error)
                    handle.Set();
            };
        }

        void CheckFail()
        {
            Assert.IsTrue(handle.WaitOne(5000, true), "Failure was not handled");
        }
    }
}
