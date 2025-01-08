#region copyright
// <copyright file="ValueSourceChanged.cs" company="Christopher McNeely">
// The MIT License (MIT)
// Copyright (c) Christopher McNeely
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
#endregion

using System;
using System.Collections.Generic;

namespace Evands.EPS.Lists
{
    /// <summary>
    /// Provides information regarding value source changes.
    /// </summary>
    /// <typeparam name="T">The type of object the value source contains.</typeparam>
    /// <seealso cref="System.EventArgs" />
    public class ValueSourceChanged<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueSourceChanged{T}"/> class.
        /// </summary>
        /// <param name="newSource">The new source.</param>
        /// <param name="oldSource">The old source.</param>
        public ValueSourceChanged(IEnumerable<T> newSource, IEnumerable<T> oldSource)
        {
            NewSource = newSource;
            OldSource = oldSource;
        }

        /// <summary>
        /// Gets the new source.
        /// </summary>
        /// <value>The new source.</value>
        public IEnumerable<T> NewSource { get; private set; }

        /// <summary>
        /// Gets the old source.
        /// </summary>
        /// <value>The old source.</value>
        public IEnumerable<T> OldSource { get; private set; }
    }
}