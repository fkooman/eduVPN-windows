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
            var instance_uri = InstanceSourceURI.Text;

            if (!eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory.Contains(instance_uri))
                eduVPN.Client.Properties.Settings.Default.CustomInstanceHistory.Insert(0, instance_uri);
        }

        #endregion
    }
}