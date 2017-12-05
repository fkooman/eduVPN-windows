﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Country selection wizard page
    /// </summary>
    public class AuthenticatingCountrySelectPage : AuthenticatingInstanceSelectPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a country selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public AuthenticatingCountrySelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}