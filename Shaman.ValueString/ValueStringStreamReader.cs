using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
#if !NET35

namespace Shaman.Runtime
{
    public class ValueStringStreamReader : IDisposable
    {
        private MultiValueStringBuilder mv;
        private Stream _stream;
        private Encoding _encoding;
        internal const int DefaultBufferSize = 1024;  // Byte buffer size

        public ValueStringStreamReader(Stream stream, Encoding encoding, MultiValueStringBuilder mv)
            : this(stream, encoding, true, DefaultBufferSize, false, mv)
        {

        }

        public ValueStringStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen, MultiValueStringBuilder mv)
        {
            this.mv = mv;
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
        }
        private const int MinBufferSize = 128;

        private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            _stream = stream;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];
            _byteLen = 0;
            _bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;

            // Encoding.GetPreamble() always allocates and returns a new byte[] array for
            // encodings that have a preamble.
            // We can avoid repeated allocations for the default and commonly used Encoding.UTF8
            // encoding by using our own private cached instance of the UTF8 preamble.
            // We specifically look for Encoding.UTF8 because we know it has a preamble,
            // whereas other instances of UTF8Encoding may not have a preamble enabled, and
            // there's no public way to tell if the preamble is enabled for an instance other
            // than calling GetPreamble(), which we're trying to avoid.
            // This means that other instances of UTF8Encoding are excluded from this optimization.
            _preamble = object.ReferenceEquals(encoding, Encoding.UTF8) ?
                (s_utf8Preamble ?? (s_utf8Preamble = encoding.GetPreamble())) :
                encoding.GetPreamble();

            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = !leaveOpen;
        }
        private static byte[] s_utf8Preamble;
        private bool _closable;
        private int _charPos;
        private int _charLen;
        private List<ValueString> valueStrings = new List<ValueString>();
        public ValueString? ReadLine()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException();
            }

            //CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                {
                    return null;
                }
            }

            valueStrings.Clear();
            do
            {
                int i = _charPos;
                do
                {
                    char ch = _charBuffer[i];
                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
                    if (ch == '\r' || ch == '\n')
                    {
                        
                        valueStrings.Add(mv.CreateValueString(_charBuffer, _charPos, i - _charPos));
                        
                        _charPos = i + 1;
                        if (ch == '\r' && (_charPos < _charLen || ReadBuffer() > 0))
                        {
                            if (_charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                            }
                        }
                        return mv.Concatenate(valueStrings);
                    }
                    i++;
                } while (i < _charLen);
                i = _charLen - _charPos;
                valueStrings.Add(mv.CreateValueString(_charBuffer, _charPos, i));
            } while (ReadBuffer() > 0);
            return mv.Concatenate(valueStrings);
        }

        public int Peek()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException();
            }

            //CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (_isBlocked || ReadBuffer() == 0)
                {
                    return -1;
                }
            }
            return _charBuffer[_charPos];
        }

        public int Read()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException();
            }

            //CheckAsyncTaskInProgress();

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                {
                    return -1;
                }
            }
            int result = _charBuffer[_charPos];
            _charPos++;
            return result;
        }

        public int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer");
            }
            if (index < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException("Argument_InvalidOffLen");
            }

            if (_stream == null)
            {
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");
            }

           // CheckAsyncTaskInProgress();

            int charsRead = 0;
            // As a perf optimization, if we had exactly one buffer's worth of 
            // data read in, let's try writing directly to the user's buffer.
            bool readToUserBuffer = false;
            while (count > 0)
            {
                int n = _charLen - _charPos;
                if (n == 0)
                {
                    n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                }
                if (n == 0)
                {
                    break;  // We're at EOF
                }
                if (n > count)
                {
                    n = count;
                }
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(_charBuffer, _charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    _charPos += n;
                }

                charsRead += n;
                count -= n;
                // This function shouldn't block for an indefinite amount of time,
                // or reading from a network stream won't work right.  If we got
                // fewer bytes than we requested, then we want to break right here.
                if (_isBlocked)
                {
                    break;
                }
            }

            return charsRead;
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            int charsRead = 0;

            // As a perf optimization, we can decode characters DIRECTLY into a
            // user's char[].  We absolutely must not write more characters 
            // into the user's buffer than they asked for.  Calculating 
            // encoding.GetMaxCharCount(byteLen) each time is potentially very 
            // expensive - instead, cache the number of chars a full buffer's 
            // worth of data may produce.  Yes, this makes the perf optimization 
            // less aggressive, in that all reads that asked for fewer than AND 
            // returned fewer than _maxCharsPerBuffer chars won't get the user 
            // buffer optimization.  This affects reads where the end of the
            // Stream comes in the middle somewhere, and when you ask for 
            // fewer chars than your buffer could produce.
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                Debug.Assert(charsRead == 0);

                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempt to detect preamble that needs to be decoded now
                        if (_byteLen > 0)
                        {
                            if (readToUserBuffer)
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                                _charLen = 0;  // StreamReader's buffer is empty.
                            }
                            else
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                                _charLen += charsRead;  // Number of chars in StreamReader's buffer.
                            }
                        }

                        return charsRead;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");

                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)  // EOF
                    {
                        break;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will change byteLen.
                _isBlocked = (_byteLen < _byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user supplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                // Note: we don't need to recompute readToUserBuffer optimization as IsPreamble
                // doesn't change the encoding or affect _maxCharsPerBuffer
                if (IsPreamble())
                {
                    continue;
                }

                // On the first call to ReadBuffer, if we're supposed to detect the encoding, do it.
                if (_detectEncoding && _byteLen >= 2)
                {
                    DetectEncoding();
                    // DetectEncoding changes some buffer state.  Recompute this.
                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                _charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                    _charLen = 0;  // StreamReader's buffer is empty.
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                    _charLen += charsRead;  // Number of chars in StreamReader's buffer.
                }
            } while (charsRead == 0);

            _isBlocked &= charsRead < desiredChars;

            //Console.WriteLine("ReadBuffer: charsRead: "+charsRead+"  readToUserBuffer: "+readToUserBuffer);
            return charsRead;
        }


        public ValueString ReadToEnd()
        {
            if (_stream == null)
            {
                throw new InvalidOperationException();
            }

            //CheckAsyncTaskInProgress();

            // Call ReadBuffer, then pull data out of charBuffer.

            int? streamLength = null;
            try
            {
                streamLength = (int)(_stream.Length - _stream.Position) + this._byteLen;
            }
            catch
            {
            }
            if (streamLength != null)
                mv.EnsureSpace(this._encoding.GetMaxCharCount(streamLength.Value));
            else
                mv.EnsureSpace(_charBuffer.Length * 4);
            
            var parts = new List<ValueString>();

            do
            {
                var p = mv.CreateValueString(_charBuffer, _charPos, _charLen - _charPos);
                parts.Add(p);
                _charPos = _charLen;  // Note we consumed these characters
                ReadBuffer();
            } while (_charLen > 0);
            return mv.Concatenate(parts);
        }



        private bool _checkPreamble;
        private int _byteLen;
        private int _bytePos;
        private char[] _charBuffer;
        private int _maxCharsPerBuffer;
        private byte[] _byteBuffer;
        private bool _detectEncoding;
        private bool _isBlocked;
        private byte[] _preamble;
        private Decoder _decoder;

        internal virtual int ReadBuffer()
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
            {
                _byteLen = 0;
            }

            do
            {
                if (_checkPreamble)
                {
                    Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
                    Debug.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (len == 0)
                    {
                        // EOF but we might have buffered bytes from previous 
                        // attempt to detect preamble that needs to be decoded now
                        if (_byteLen > 0)
                        {
                            _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
                            // Need to zero out the byteLen after we consume these bytes so that we don't keep infinitely hitting this code path
                            _bytePos = _byteLen = 0;
                        }

                        return _charLen;
                    }

                    _byteLen += len;
                }
                else
                {
                    Debug.Assert(_bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    Debug.Assert(_byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");

                    if (_byteLen == 0)  // We're at EOF
                    {
                        return _charLen;
                    }
                }

                // _isBlocked == whether we read fewer bytes than we asked for.
                // Note we must check it here because CompressBuffer or 
                // DetectEncoding will change byteLen.
                _isBlocked = (_byteLen < _byteBuffer.Length);

                // Check for preamble before detect encoding. This is not to override the
                // user supplied Encoding for the one we implicitly detect. The user could
                // customize the encoding which we will loose, such as ThrowOnError on UTF8
                if (IsPreamble())
                {
                    continue;
                }

                // If we're supposed to detect the encoding and haven't done so yet,
                // do it.  Note this may need to be called more than once.
                if (_detectEncoding && _byteLen >= 2)
                {
                    DetectEncoding();
                }

                _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
            } while (_charLen == 0);
            //Console.WriteLine("ReadBuffer called.  chars: "+charLen);
            return _charLen;
        }
        private bool IsPreamble()
        {
            if (!_checkPreamble)
            {
                return _checkPreamble;
            }

            Debug.Assert(_bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?");
            int len = (_byteLen >= (_preamble.Length)) ? (_preamble.Length - _bytePos) : (_byteLen - _bytePos);

            for (int i = 0; i < len; i++, _bytePos++)
            {
                if (_byteBuffer[_bytePos] != _preamble[_bytePos])
                {
                    _bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            Debug.Assert(_bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");

            if (_checkPreamble)
            {
                if (_bytePos == _preamble.Length)
                {
                    // We have a match
                    CompressBuffer(_preamble.Length);
                    _bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }
        private void CompressBuffer(int n)
        {
            Debug.Assert(_byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?");
            Buffer.BlockCopy(_byteBuffer, n, _byteBuffer, 0, _byteLen - n);
            _byteLen -= n;
        }

        private void DetectEncoding()
        {
            if (_byteLen < 2)
            {
                return;
            }
            _detectEncoding = false;
            bool changedEncoding = false;
            if (_byteBuffer[0] == 0xFE && _byteBuffer[1] == 0xFF)
            {
                // Big Endian Unicode

                _encoding = Encoding.BigEndianUnicode;
                CompressBuffer(2);
                changedEncoding = true;
            }

            else if (_byteBuffer[0] == 0xFF && _byteBuffer[1] == 0xFE)
            {
                // Little Endian Unicode, or possibly little endian UTF32
                if (_byteLen < 4 || _byteBuffer[2] != 0 || _byteBuffer[3] != 0)
                {
                    _encoding = Encoding.Unicode;
                    CompressBuffer(2);
                    changedEncoding = true;
                }
            }

            else if (_byteLen >= 3 && _byteBuffer[0] == 0xEF && _byteBuffer[1] == 0xBB && _byteBuffer[2] == 0xBF)
            {
                // UTF-8
                _encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (_byteLen == 2)
            {
                _detectEncoding = true;
            }
            // Note: in the future, if we change this algorithm significantly,
            // we can support checking for the preamble of the given encoding.

            if (changedEncoding)
            {
                _decoder = _encoding.GetDecoder();
                _maxCharsPerBuffer = _encoding.GetMaxCharCount(_byteBuffer.Length);
                _charBuffer = new char[_maxCharsPerBuffer];
            }
        }
        public void Dispose()
        {
            // Dispose of our resources if this StreamReader is closable.
            // Note that Console.In should be left open.
            try
            {
                // Note that Stream.Close() can potentially throw here. So we need to 
                // ensure cleaning up internal resources, inside the finally block.  
                if (_closable && (_stream != null))
                {
                    _stream.Dispose();
                }
            }
            finally
            {
                if (_closable && (_stream != null))
                {
                    _stream = null;
                    _encoding = null;
                    _decoder = null;
                    _byteBuffer = null;
                    _charBuffer = null;
                    _charPos = 0;
                    _charLen = 0;
                    //base.Dispose(disposing);
                }
            }
        }
    }
}
#endif