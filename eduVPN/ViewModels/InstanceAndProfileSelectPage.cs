﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Net;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance and profile selection wizard page
    /// </summary>
    public class InstanceAndProfileSelectPage : ProfileSelectBasePage
    {
        #region Properties

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set {
                _selected_instance = value;
                RaisePropertyChanged();

                ProfileList = new JSON.Collection<Models.ProfileInfo>();
                if (_selected_instance != null)
                {
                    new Thread(new ThreadStart(
                        () =>
                        {
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                            try
                            {
                                // Get and load API endpoints.
                                var uri_builder = new UriBuilder(_selected_instance.Base);
                                    uri_builder.Path += "info.json";
                                    var api = new Models.InstanceEndpoints();
                                    api.LoadJSON(JSON.Response.Get(
                                        uri: uri_builder.Uri,
                                        ct: ConnectWizard.Abort.Token).Value, ConnectWizard.Abort.Token);

                                // Set selected instance API endpoints.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => SelectedInstanceEndpoints = api));

                                // Get and load profile list.
                                var profile_list = new JSON.Collection<Models.ProfileInfo>();
                                    try
                                    {
                                        profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                                            uri: api.ProfileList,
                                            token: Parent.AccessToken,
                                            ct: ConnectWizard.Abort.Token).Value, "profile_list", ConnectWizard.Abort.Token);
                                    }
                                    catch (WebException ex)
                                    {
                                    // Access token rejected (401) => Redirect back to authorization page.
                                    if (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Unauthorized)
                                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.CurrentPage = Parent.AuthorizationPage));
                                        else
                                            throw;
                                    }

                                // Send the loaded profile list back to the UI thread.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                    {
                                        ProfileList = profile_list;
                                        ErrorMessage = null;
                                    }));
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex)
                            {
                                // Notify the sender the API endpoints loading failed.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                            }
                            finally
                            {
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                            }
                        })).Start();
                }
            }
        }
        private Models.InstanceInfo _selected_instance;

        /// <summary>
        /// Selected eduVPN instance API endpoints
        /// </summary>
        public Models.InstanceEndpoints SelectedInstanceEndpoints
        {
            get { return _selected_instance_endpoints; }
            set { _selected_instance_endpoints = value; RaisePropertyChanged(); }
        }
        private Models.InstanceEndpoints _selected_instance_endpoints;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceAndProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            if (Parent.InstanceList is Models.InstanceInfoFederatedList)
            {
                // Federated instance list does not favour any particular instance to connect to.
                SelectedInstance = null;
            }
            else
            {
                // By default, select the same connecting instance as authenticating instance.
                SelectedInstance = Parent.AuthenticatingInstance;
            }
        }

        protected override void DoConnectSelectedProfile()
        {
            Parent.ConnectingInstance = SelectedInstance;
            Parent.ConnectingEndpoints = SelectedInstanceEndpoints;

            base.DoConnectSelectedProfile();
        }

        protected override bool CanConnectSelectedProfile()
        {
            return
                SelectedInstance != null &&
                SelectedInstanceEndpoints != null &&
                base.CanConnectSelectedProfile();
        }

        #endregion
    }
}