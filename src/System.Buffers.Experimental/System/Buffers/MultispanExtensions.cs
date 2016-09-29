﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Sequences;
using System.Text;

namespace System.Buffers
{
    public static class MultispanExtensions
    {
        public static bool TryParseUInt32<TMultispan>(this TMultispan bytes, EncodingData encoding, out uint value, out int consumed) where TMultispan : ISpanSequence<byte>
        {
            Position position = Position.First;
            Span<byte> first;
            if (!bytes.TryGet(ref position, out first, advance: true)) {
                throw new ArgumentException("bytes cannot be empty");
            }

            if (!PrimitiveParser.TryParse(first, EncodingData.Encoding.Utf8, out value, out consumed)) {
                return false; // TODO: maybe we should continue in some cases, e.g. if the first span ends in a decimal separator
                                // ... cont, maybe consumed could be set even if TryParse returns false
            }
            if (position.Equals(Position.AfterLast) || first.Length > consumed) {
                return true;
            }

            Span<byte> second;
            if (!bytes.TryGet(ref position, out second, advance: true)) {
                throw new ArgumentException("bytes cannot be empty");
            }

            Span<byte> temp;
            int numberOfBytesFromSecond = second.Length;
            if (numberOfBytesFromSecond > 64) numberOfBytesFromSecond = 64;
            var tempBufferLength = first.Length + numberOfBytesFromSecond;
            if (tempBufferLength > 128) {
                temp = new byte[tempBufferLength];
            } else {
                unsafe
                {
                    byte* data = stackalloc byte[tempBufferLength];
                    temp = new Span<byte>(data, tempBufferLength);
                }
            }

            first.TryCopyTo(temp);

            second.Slice(0, numberOfBytesFromSecond).TryCopyTo(temp.Slice(first.Length));

            if (!PrimitiveParser.TryParse(temp, EncodingData.Encoding.Utf8, out value, out consumed)) {
                return false;
            }

            if (position.Equals(Position.AfterLast) || temp.Length > consumed) {
                return true;
            }

            throw new NotImplementedException();
        }
    }

}
