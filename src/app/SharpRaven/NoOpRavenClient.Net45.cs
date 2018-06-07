#region License

// Copyright (c) 2014 The Sentry Team and individual contributors.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
//
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
//
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
//
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

#if !NET40 && !NET35

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SharpRaven.Data;

namespace SharpRaven
{
    /// <summary>
    /// Empty (no-op) implementation for the Raven Client for use in dependency injection
    /// and other places when a silent operation is needed.
    /// </summary>
    public partial class NoOpRavenClient
    {
        /// <summary>Captures the event.</summary>
        /// <param name="event">The event.</param>
        /// <returns>
        /// The <see cref="JsonPacket.EventID" /> of the successfully captured <paramref name="exception" />, or <c>null</c> if it fails.
        /// </returns>
        public async Task<string> CaptureAsync(SentryEvent @event)
        {
            return await Task.FromResult(Guid.NewGuid().ToString("n"));
        }


        /// <summary>
        /// Captures the <see cref="Exception" />.
        /// </summary>
        /// <param name="exception">The <see cref="Exception" /> to capture.</param>
        /// <param name="message">The optional message to capture. Default: <see cref="Exception.Message" />.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="exception" />. Default: <see cref="ErrorLevel.Error"/>.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="exception" /> with.</param>
        /// <param name="fingerprint">The custom fingerprint to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="exception" />.</param>
        /// <returns>
        /// The <see cref="JsonPacket.EventID" /> of the successfully captured <paramref name="exception" />, or <c>null</c> if it fails.
        /// </returns>
        [Obsolete("Use CaptureAsync(SentryEvent) instead.")]
        public async Task<string> CaptureExceptionAsync(Exception exception,
                                                        SentryMessage message = null,
                                                        ErrorLevel level = ErrorLevel.Error,
                                                        IDictionary<string, string> tags = null,
                                                        string[] fingerprint = null,
                                                        object extra = null)
        {
            return await Task.FromResult(Guid.NewGuid().ToString("n"));
        }


        /// <summary>
        /// Captures the message.
        /// </summary>
        /// <param name="message">The message to capture.</param>
        /// <param name="level">The <see cref="ErrorLevel" /> of the captured <paramref name="message"/>. Default <see cref="ErrorLevel.Info"/>.</param>
        /// <param name="tags">The tags to annotate the captured <paramref name="message"/> with.</param>
        /// <param name="fingerprint">The custom fingerprint to annotate the captured <paramref name="message" /> with.</param>
        /// <param name="extra">The extra metadata to send with the captured <paramref name="message"/>.</param>
        /// <returns>
        /// The <see cref="JsonPacket.EventID"/> of the successfully captured <paramref name="message"/>, or <c>null</c> if it fails.
        /// </returns>
        [Obsolete("Use CaptureAsync(SentryEvent) instead.")]
        public async Task<string> CaptureMessageAsync(SentryMessage message,
                                                      ErrorLevel level = ErrorLevel.Info,
                                                      IDictionary<string, string> tags = null,
                                                      string[] fingerprint = null,
                                                      object extra = null)
        {
            return await Task.FromResult(Guid.NewGuid().ToString("n"));
        }

        /// <summary>Sends the specified user feedback to Sentry</summary>
        /// <returns>An empty string if succesful, otherwise the server response</returns>
        /// <param name="feedback">The user feedback to send</param>
        public async Task<string> SendUserFeedbackAsync(SentryUserFeedback feedback)
        {
            return await Task.FromResult(string.Empty);
        }
    }
}

#endif
