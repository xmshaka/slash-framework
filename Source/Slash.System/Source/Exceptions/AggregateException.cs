﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AggregateException.cs" company="Slash Games">
//   Copyright (c) Slash Games. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Slash.SystemExt.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    ///   Wraps multiple inner exceptions.
    ///   http://msdn.microsoft.com/en-us/magazine/ee321571.aspx
    /// </summary>
    [DebuggerDisplay("Count = {InnerExceptions.Count}")]
    public class AggregateException : Exception
    {
        #region Constants

        private const string DefaultMessage = "Multiple exceptions have occurred.";

        #endregion

        #region Fields

        private readonly IList<Exception> innerExceptions;

        #endregion

        #region Constructors and Destructors

        public AggregateException()
            : this(new List<Exception>())
        {
        }

        public AggregateException(params Exception[] innerExceptions)
            : this(new List<Exception>(innerExceptions))
        {
        }

        public AggregateException(IEnumerable<Exception> innerExceptions)
            : this(DefaultMessage, innerExceptions)
        {
        }

        public AggregateException(string message)
            : this(message, new List<Exception>())
        {
        }

        public AggregateException(string message, Exception innerException)
            : this(message, new List<Exception> { innerException })
        {
        }

        public AggregateException(string message, params Exception[] innerExceptions)
            : this(message, new List<Exception>(innerExceptions))
        {
        }

        public AggregateException(string message, IEnumerable<Exception> innerExceptions)
            : base(message, innerExceptions.FirstOrDefault())
        {
            this.innerExceptions = innerExceptions.ToList();
        }

        #endregion

        #region Public Properties

        public ReadOnlyCollection<Exception> InnerExceptions
        {
            get
            {
                return new ReadOnlyCollection<Exception>(this.innerExceptions);
            }
        }

        #endregion
    }
}