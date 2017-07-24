﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.JSON;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance selection base wizard page
    /// </summary>
    public class InstanceSelectPage : ConnectWizardPage
    {
        #region Fields

        /// <summary>
        /// Instance directory URI ID as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        private string _instance_directory_id;

        /// <summary>
        /// Cached instance list
        /// </summary>
        private Dictionary<string, object> _cache;

        /// <summary>
        /// Should the list of instances have "Other (not listed)" entry appended?
        /// </summary>
        private bool _has_custom;

        #endregion

        #region Properties

        /// <summary>
        /// List of available instances
        /// </summary>
        public JSON.InstanceList InstanceList
        {
            get { return _instance_list; }
            set { _instance_list = value; RaisePropertyChanged(); }
        }
        private JSON.InstanceList _instance_list;

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public JSON.Instance SelectedInstance
        {
            get { return _selected_instance; }
            set
            {
                _selected_instance = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)AuthorizeSelectedInstance).RaiseCanExecuteChanged();
            }
        }
        private JSON.Instance _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public ICommand AuthorizeSelectedInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            // Set busy flag.
                            IsBusy = true;

                            try
                            {
                                // Save selected instance.
                                Parent.Instance = SelectedInstance;

                                if (SelectedInstance.IsCustom)
                                    Parent.CurrentPage = Parent.CustomInstancePage;
                                else
                                {
                                    // Schedule get API endpoints.
                                    var uri_builder = new UriBuilder(Parent.Instance.Base);
                                    uri_builder.Path += "info.json";
                                    var api_get_task = JSON.Response.GetAsync(
                                        uri_builder.Uri,
                                        null,
                                        null,
                                        null,
                                        _abort.Token);

                                    // Try to restore the access token from the settings.
                                    Parent.AccessToken = null;
                                    var now = DateTime.Now;
                                    try
                                    {
                                        var at = Properties.Settings.Default.AccessTokens[Parent.Instance.Base.AbsoluteUri];
                                        if (at != null)
                                            Parent.AccessToken = AccessToken.FromBase64String(at);

                                        if (Parent.AccessToken == null && InstanceList.AuthType == InstanceList.AuthorizationType.Distributed)
                                        {
                                            // Try to find the most appropriate token from any instance.
                                            foreach (var inst in InstanceList)
                                            {
                                                try
                                                {
                                                    at = Properties.Settings.Default.AccessTokens[inst.Base.AbsoluteUri];
                                                    if (at != null)
                                                    {
                                                        var token = AccessToken.FromBase64String(at);
                                                        if (Parent.AccessToken == null)
                                                        {
                                                            // The first token was found.
                                                            Parent.AccessToken = token;
                                                        }
                                                        else if (
                                                            Parent.AccessToken.Expires.HasValue && Parent.AccessToken.Expires.Value <= now &&
                                                            !(token.Expires.HasValue && token.Expires.Value <= now))
                                                        {
                                                            // The previous token found has expired, but the one found now hasn't yet.
                                                            Parent.AccessToken = token;
                                                        }
                                                        // else if ... If we have any other means of prioritizing tokens, we should implement them here.
                                                    }
                                                }
                                                catch (Exception) { }
                                            }
                                        }
                                    }
                                    catch (Exception) { }

                                    // Load API endpoints
                                    var api = new JSON.API();
                                    api.LoadJSON((await api_get_task).Value);
                                    Parent.Endpoints = api;

                                    if (Parent.AccessToken != null && Parent.AccessToken.Expires.HasValue && Parent.AccessToken.Expires.Value <= now)
                                    {
                                        // The access token expired. Try refreshing it.
                                        try
                                        {
                                            Parent.AccessToken = await Parent.AccessToken.RefreshTokenAsync(
                                                Parent.Endpoints.TokenEndpoint,
                                                null,
                                                _abort.Token);
                                        }
                                        catch (Exception)
                                        {
                                            Parent.AccessToken = null;
                                        }
                                    }

                                    if (Parent.AccessToken != null)
                                        Parent.CurrentPage = Parent.ProfileSelectPage;
                                    else
                                        Parent.CurrentPage = Parent.AuthorizationPage;
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage = ex.Message;
                            }
                            finally
                            {
                                // Clear busy flag.
                                IsBusy = false;
                            }
                        },

                        // canExecute
                        () => SelectedInstance != null);
                }
                return _authorize_instance;
            }
        }
        private ICommand _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceSelectPage(ConnectWizard parent, string instance_directory_id, bool has_custom) :
            base(parent)
        {
            _instance_directory_id = instance_directory_id;
            _has_custom = has_custom;

            try
            {
                // Restore instance list cache.
                _cache = (Dictionary<string, object>)eduJSON.Parser.Parse((string)Properties.Settings.Default[_instance_directory_id + "Cache"]);
            }
            catch (Exception)
            {
                // Revert cache to default initial value.
                _cache = new Dictionary<string, object>()
                {
                    { "instances", new List<object>() },
                    { "seq", 0 }
                };
            }

            // Initialize instance list.
            InstanceList = new JSON.InstanceList();
            InstanceList.Load(_cache);
            if (_has_custom)
            {
                // Append "Other instance" entry.
                InstanceList.Add(new JSON.Instance()
                {
                    DisplayName = Resources.Strings.CustomInstance,
                    IsCustom = true,
                });
            }

            // Launch instance list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(InstanceListLoader));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads instance list from web service.
        /// </summary>
        private void InstanceListLoader(object param)
        {
            JSON.Response json = null;

            for (;;)
            {
                try
                {
                    // Set busy flag (in the UI thread).
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = true));

                    try
                    {
                        // Get instance list.
                        json = JSON.Response.Get(
                            new Uri((string)Properties.Settings.Default[_instance_directory_id]),
                            null,
                            null,
                            Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id + "PubKey"]),
                            _abort.Token,
                            json);

                        if (json.IsFresh)
                        {
                            // Parse instance list.
                            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(json.Value, _abort.Token);

                            // Load instance list.
                            var instance_list = new JSON.InstanceList();
                            instance_list.Load(obj);

                            if (_has_custom)
                            {
                                // Append "Other instance" entry.
                                instance_list.Add(new JSON.Instance()
                                {
                                    DisplayName = Resources.Strings.CustomInstance,
                                    IsCustom = true,
                                });
                            }

                            // Send the loaded instance list back to the UI thread.
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                InstanceList = instance_list;
                                ErrorMessage = null;
                            }));

                            try
                            {
                                // If we got here, the loaded instance list is (probably) OK.
                                bool update_cache = false;
                                try { update_cache = eduJSON.Parser.GetValue<int>(obj, "seq") >= eduJSON.Parser.GetValue<int>(_cache, "seq"); }
                                catch (Exception) { update_cache = true; }
                                if (update_cache)
                                {
                                    // Update cache.
                                    _cache = obj;
                                    Properties.Settings.Default[_instance_directory_id + "Cache"] = json.Value;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = false));
                    }

                    // Wait for five minutes.
                    if (_abort.Token.WaitHandle.WaitOne(1000 * 60 * 5))
                        break;
                }
                catch (OperationCanceledException)
                {
                    // The load was aborted.
                    break;
                }
                catch (Exception ex)
                {
                    // Notify the sender the instance list loading failed.
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));

                    // Wait for ten seconds.
                    if (_abort.Token.WaitHandle.WaitOne(1000 * 10))
                        break;

                    // Make it a clean start.
                    json = null;
                    continue;
                }
            }
        }

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.AccessTypePage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        public override void OnActivate()
        {
            // Reset selected instance, to prevent automatic continuation to
            // CustomInstance/Authorization page.
            SelectedInstance = null;

            base.OnActivate();
        }

        #endregion
    }
}
