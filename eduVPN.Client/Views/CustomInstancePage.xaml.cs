﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for CustomInstancePage.xaml
    /// </summary>
    public partial class CustomInstancePage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public CustomInstancePage()
        {
            if (eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory == null)
                eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory = new System.Collections.Specialized.StringCollection();

            InitializeComponent();
        }

        #endregion

        #region Methods

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var hostname = InstanceHostname.Text;

            if (!eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory.Contains(hostname))
                eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory.Insert(0, hostname);
        }

        #endregion
    }
}
