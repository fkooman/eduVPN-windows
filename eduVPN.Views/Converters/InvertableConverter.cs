﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.Diagnostics;

namespace eduVPN.Converters
{
    /// <summary>
    /// Base class for invertable converters
    /// </summary>
    public class InvertableConverter : BindableBase
    {
        #region Properties

        /// <summary>
        /// Invert expected value
        /// </summary>
        public bool Invert
        {
            get { return _Invert; }
            set { SetProperty(ref _Invert, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _Invert = false;

        #endregion
    }
}
